using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Products.Read.API.Abstractions;
using Products.Read.API.Auth;
using Products.Read.API.Configuration;
using Products.Read.API.Infrastructure.Data;
using Products.Read.API.Infrastructure.Repositories;
using Products.Read.API.MessageConsumers;
using Products.Read.API.MessageQueues;
using Products.Read.API.MessageServices;
using Products.Read.API.QueryServices;
using Products.Read.Validators;
using System.Security.Authentication;

namespace Products.Read.API
{
    public static class CompositionRoot
    {
        public static IServiceCollection ComposeApplication(this IServiceCollection services, IWebHostEnvironment env)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            // Add database context and cache
            if (env.IsDevelopment())
            {
                services.AddDbContext<ProductsReadDbContext>(options =>
                    options.UseSqlServer(configuration["LOCAL_SQL_CONNECTIONSTRING"]));
                // services.AddDistributedMemoryCache();
            }
            else
            {
                services.AddDbContext<ProductsReadDbContext>(options =>
                    options.UseSqlServer(configuration["AZURE_SQL_CONNECTIONSTRING"]));
                //services.AddStackExchangeRedisCache(options =>
                //{
                //    options.Configuration = builder.Configuration["AZURE_REDIS_CONNECTIONSTRING"];
                //    options.InstanceName = "ProductsReadInstance";
                //});
            }

            /// Previous before configure for Azure deployment
            //services.AddDbContext<ProductsReadDbContext>(options =>
            //{
            //    // options.UseSqlServer(configuration.GetConnectionString("LocalDevelopmentConnectionString"));
            //    // options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
            //});

            // Register FluentValidation validators
            services.AddValidatorsFromAssemblyContaining<ThrowExceptionDtoValidator>();

            services.AddScoped<IMessageQueue, ProductMessageQueue>();
            services.AddScoped<IProductMessageProcessor, ProductMessageProcessor>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductQueryService, ProductQueryService>();
            services.AddScoped<ITokenDecoder, TokenDecoder>();

            services.AddOptions<CloudAMQPSettings>().Configure<IConfiguration>((options, config) =>
            {
                config.GetSection(nameof(CloudAMQPSettings)).Bind(options);
            });

            // Register CloudAMQP related services
            string amqpUrl = configuration.GetSection("CloudAMQPSettings:Url").Value ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            string amqpUsername = configuration.GetSection("CloudAMQPSettings:UserVhost").Value ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            string amqpPassword = configuration.GetSection("CloudAMQPSettings:Password").Value ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");

            services.AddMassTransit(x =>
            {
                x.AddConsumer<ProductAddedConsumer>();
                x.AddConsumer<StatusUpdateConsumer>();
                x.AddConsumer<DocumentAddedConsumer>();
                x.AddConsumer<ImageAddedConsumer>();
                x.AddConsumer<DocumentDeletedConsumer>();
                x.AddConsumer<ImageDeletedConsumer>();
                x.AddConsumer<DataPurgedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(amqpUrl), h =>
                    {
                        h.Username(amqpUsername);
                        h.Password(amqpPassword);

                        h.UseSsl(s =>
                        {
                            s.Protocol = SslProtocols.Tls12;
                        });
                    });
                    cfg.ReceiveEndpoint("ProductsReadApi1Queue", e =>
                    {
                        e.ConfigureConsumeTopology = false; // explicit is safer for versioning

                        e.ConfigureConsumer<ProductAddedConsumer>(context);
                        e.ConfigureConsumer<StatusUpdateConsumer>(context);
                        e.ConfigureConsumer<DocumentAddedConsumer>(context);
                        e.ConfigureConsumer<ImageAddedConsumer>(context);
                        e.ConfigureConsumer<DocumentDeletedConsumer>(context);
                        e.ConfigureConsumer<ImageDeletedConsumer>(context);
                        e.ConfigureConsumer<DataPurgedConsumer>(context);

                        // Robustness: retry with jitter + immediate faults to _error queue if exhausted
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                        e.PrefetchCount = 4;    // 16;    // 16;
                        e.ConcurrentMessageLimit = 1;   // 8;  // 8;
                    });
                });
            });

            return services;
        }
    }
}

