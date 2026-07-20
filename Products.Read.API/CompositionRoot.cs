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
        public static IServiceCollection ComposeApplication(this IServiceCollection services, string? environmentName)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            if (environmentName == "Development")
            {
                services.AddDbContext<ProductsReadDbContext>(options =>
                    options.UseSqlServer(configuration["LOCAL_SQL_CONNECTIONSTRING"]));
            }
            else
            {
                services.AddDbContext<ProductsReadDbContext>(options =>
                    options.UseSqlServer(configuration["AZURE_SQL_READ_CONNECTIONSTRING"]));
            }

            services.AddOptions<CloudAMQPSettings>().Configure<IConfiguration>((options, config) =>
            {
                config.GetSection(nameof(CloudAMQPSettings)).Bind(options);
            });

            // Register FluentValidation validators
            services.AddValidatorsFromAssemblyContaining<ThrowExceptionDtoValidator>();

            //services.AddScoped<IMessageQueue, ProductMessageQueue>();
            services.AddScoped<IProductMessageProcessor, ProductMessageProcessor>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductQueryService, ProductQueryService>();
            services.AddScoped<ITokenDecoder, TokenDecoder>();

            return services;
        }
    }
}

