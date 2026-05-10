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
        public static IServiceCollection ComposeApplication(this IServiceCollection services)
        {
            //var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            //// Add database context and cache
            //if (env.IsDevelopment())
            //{
            //    services.AddDbContext<ProductsReadDbContext>(options =>
            //        options.UseSqlServer(configuration["LOCAL_SQL_CONNECTIONSTRING"]));
            //    // services.AddDistributedMemoryCache();
            //}
            //else
            //{
            //    services.AddDbContext<ProductsReadDbContext>(options =>
            //        options.UseSqlServer(configuration["AZURE_SQL_READ_CONNECTIONSTRING"]));
            //    //services.AddStackExchangeRedisCache(options =>
            //    //{
            //    //    options.Configuration = builder.Configuration["AZURE_REDIS_READ_CONNECTIONSTRING"];
            //    //    options.InstanceName = "ProductsReadInstance";
            //    //});
            //}

            /// Previous before configure for Azure deployment
            //services.AddDbContext<ProductsReadDbContext>(options =>
            //{
            //    // options.UseSqlServer(configuration.GetConnectionString("LocalDevelopmentConnectionString"));
            //    // options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
            //});

            services.AddOptions<CloudAMQPSettings>().Configure<IConfiguration>((options, config) =>
            {
                config.GetSection(nameof(CloudAMQPSettings)).Bind(options);
            });

            // Register FluentValidation validators
            services.AddValidatorsFromAssemblyContaining<ThrowExceptionDtoValidator>();

            services.AddScoped<IMessageQueue, ProductMessageQueue>();
            services.AddScoped<IProductMessageProcessor, ProductMessageProcessor>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductQueryService, ProductQueryService>();
            services.AddScoped<ITokenDecoder, TokenDecoder>();

            //// Register CloudAMQP related services
            //string amqpUrl = configuration.GetValue<string>("CloudAMQPSettings:Url") ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            //string amqpUsername = configuration.GetValue<string>("CloudAMQPSetting:UserVhost") ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            //string amqpPassword = configuration.GetValue<string>("CloudAMQPSettings:Password") ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");

            //string amqpUrl = configuration.GetSection("CloudAMQPSettings:Url").Value ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            //string amqpUsername = configuration.GetSection("CloudAMQPSetting:UserVhost").Value ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            //string amqpPassword = configuration.GetSection("CloudAMQPSettings:Password").Value ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");

            //// THE BELOW WORKS IN DEV AND GIT WORKFLOW BUT VALUES ARE NULL IN AZURE
            //string amqpUrl = env.IsDevelopment() ? configuration.GetSection("CloudAMQPSettings:Url").Value! : configuration["CloudAMQPSettings:Url"]! ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            //string amqpUsername = env.IsDevelopment() ? configuration.GetSection("CloudAMQPSetting:UserVhost").Value! : configuration["CloudAMQPSetting:UserVhost"]! ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            //string amqpPassword = env.IsDevelopment() ? configuration.GetSection("CloudAMQPSettings:Password").Value! : configuration["CloudAMQPSettings:Password"]! ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");

            //// TRY UNIQUE APPROACH FOR EACH ENVIRONMENT
            //string amqpUrl = env.IsDevelopment() ?
            //    configuration.GetSection("CloudAMQPSettings:Url").Value! :          // local dev
            //    configuration["CloudAMQPSettings:Url"]! ??                          // GitHub actions
            //    configuration.GetValue<string>("CloudAMQPSettings__Url") ??         // Azure App Service 
            //    throw new ArgumentNullException("Invalid Cloud AMQP URL configuration.");
            //string amqpUsername = env.IsDevelopment() ? 
            //    configuration.GetSection("CloudAMQPSetting:UserVhost").Value! :     // local dev
            //    configuration["CloudAMQPSetting:UserVhost"]! ??                     // GitHub actions
            //    configuration.GetValue<string>("CloudAMQPSetting__UserVhost") ??    // Azure App Service
            //    throw new ArgumentNullException("Invalid Cloud AMQP User VHost configuration.");
            //string amqpPassword = env.IsDevelopment() ? 
            //    configuration.GetSection("CloudAMQPSettings:Password").Value! :     // local dev
            //    configuration["CloudAMQPSettings:Password"]! ??                     // GitHub actions
            //    configuration.GetValue<string>("CloudAMQPSettings__Password")! ??   // Azure App Service
            //    throw new ArgumentNullException("Invalid Cloud AMQP Password configuration.");

            ////// THE BELOW WORKS IN DEV BUT VALUES ARE NULL IN GIT WORKFLOW
            //string amqpUrl = configuration["CLOUD_AMQP_SETTINGS_URL"] ?? throw new ArgumentNullException("Invalid Cloud AMQP URL configuration.");
            //string amqpUsername = configuration["CLOUD_AMQP_SETTINGS_USERVHOST"] ?? throw new ArgumentNullException("Invalid Cloud AMQP User VHost configuration.");
            //string amqpPassword = configuration["CLOUD_AMQP_SETTINGS_PASSWORD"] ?? throw new ArgumentNullException("Invalid Cloud AMQP Password configuration.");

            //// THE BELOW WORKS IN DEV BUT VALUES ARE NULL IN GIT WORKFLOW
            //string amqpUrl = configuration.GetValue<string>("CLOUD_AMQP_SETTINGS_URL") ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            //string amqpUsername = configuration.GetValue<string>("CLOUD_AMQP_SETTINGS_USERVHOST") ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            //string amqpPassword = configuration.GetValue<string>("CLOUD_AMQP_SETTINGS_PASSWORD") ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");

            //// THE BELOW IS A HYBRID KEY APPROACH
            //string amqpUrl = configuration.GetValue<string>("CLOUD_AMQP_SETTINGS_URL") ?? configuration["CloudAMQPSettings:Url"]! ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            //string amqpUsername = configuration.GetValue<string>("CLOUD_AMQP_SETTINGS_USERVHOST") ?? configuration["CloudAMQPSetting:UserVhost"]! ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            //string amqpPassword = configuration.GetValue<string>("CLOUD_AMQP_SETTINGS_PASSWORD") ?? configuration["CloudAMQPSettings:Password"]! ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");

            //services.AddMassTransit(x =>
            //{
            //    x.AddConsumer<ProductAddedConsumer>();
            //    x.AddConsumer<StatusUpdateConsumer>();
            //    x.AddConsumer<DocumentAddedConsumer>();
            //    x.AddConsumer<ImageAddedConsumer>();
            //    x.AddConsumer<DocumentDeletedConsumer>();
            //    x.AddConsumer<ImageDeletedConsumer>();
            //    x.AddConsumer<DataPurgedConsumer>();

            //    x.UsingRabbitMq((context, cfg) =>
            //    {
            //        cfg.Host(new Uri(amqpUrl), h =>
            //        {
            //            h.Username(amqpUsername);
            //            h.Password(amqpPassword);

            //            h.UseSsl(s =>
            //            {
            //                s.Protocol = SslProtocols.Tls12;
            //            });
            //        });
            //        cfg.ReceiveEndpoint("ProductsReadApi1Queue", e =>
            //        {
            //            e.ConfigureConsumeTopology = false; // explicit is safer for versioning

            //            e.ConfigureConsumer<ProductAddedConsumer>(context);
            //            e.ConfigureConsumer<StatusUpdateConsumer>(context);
            //            e.ConfigureConsumer<DocumentAddedConsumer>(context);
            //            e.ConfigureConsumer<ImageAddedConsumer>(context);
            //            e.ConfigureConsumer<DocumentDeletedConsumer>(context);
            //            e.ConfigureConsumer<ImageDeletedConsumer>(context);
            //            e.ConfigureConsumer<DataPurgedConsumer>(context);

            //            // Robustness: retry with jitter + immediate faults to _error queue if exhausted
            //            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
            //            e.PrefetchCount = 4;    // 16;    // 16;
            //            e.ConcurrentMessageLimit = 1;   // 8;  // 8;
            //        });
            //    });
            //});

            return services;
        }
    }
}

