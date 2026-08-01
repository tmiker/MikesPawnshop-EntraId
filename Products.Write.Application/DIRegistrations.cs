using FluentValidation;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Products.Write.Application.Abstractions;
using Products.Write.Application.Configuration;
using Products.Write.Application.CQRS.CommandHandlers;
using Products.Write.Application.CQRS.CommandResults;
using Products.Write.Application.CQRS.Commands;
using Products.Write.Application.CQRS.DevTests;
using Products.Write.Application.CQRS.Dispatchers;
using Products.Write.Application.EventManagement;
using Products.Write.Application.Services;
using Products.Write.Application.Validators;
using System.Security.Authentication;

namespace Products.Write.Application
{
    public static class DIRegistrations
    {
        public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
        {
            // Register configurations
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            string amqpUrl = configuration.GetSection("CLOUD_AMQP_SETTINGS_URL").Value ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            string amqpUsername = configuration.GetSection("CLOUD_AMQP_SETTINGS_USERVHOST").Value ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");
            string amqpPassword = configuration.GetSection("CLOUD_AMQP_SETTINGS_PASSWORD").Value ?? throw new ArgumentNullException("Invalid Cloud AMQP configuration.");

            // Register messaging services
            services.AddMassTransit(x =>
            {
                string licenseKey = "";

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
                });
            });

            // Register FluentValidation validators
            services.AddValidatorsFromAssemblyContaining<AddProductDtoValidator>();

            // Register the SingleThreadedEventAggregator as a singleton
            services.AddScoped<SingleThreadedEventAggregator>();
            // Register ProductEventHandlers 
            services.AddScoped<ProductEventHandlers>();

            // Build the service provider and register the event handlers with the event aggregator as IEventAggregator
            services.AddScoped(serviceProvider =>
            {
                IRegisterableEventHandlers handlers = serviceProvider.GetRequiredService<ProductEventHandlers>();
                IEventAggregator aggregator = serviceProvider.GetRequiredService<SingleThreadedEventAggregator>();
                aggregator.Register(handlers);
                return aggregator;
            });

            // Register Azure Blob Storage Services
            services.AddScoped<IAzureStorageService, AzureStorageService>();
            services.AddScoped<IImageResizeHelper, ImageResizeHelper>();

            // Register other services
            services.AddScoped<IDevQueryService, DevQueryService>();

            return services;
        }
    }
}
