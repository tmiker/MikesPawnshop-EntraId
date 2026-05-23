using Microsoft.EntityFrameworkCore;
using Products.Write.API.Auth;
using Products.Write.API.Configuration;
using Products.Write.Application;
using Products.Write.Infrastructure;
using Products.Write.Infrastructure.DataAccess;

namespace Products.Write.API
{
    public static class CompositionRoot
    {
        public static IServiceCollection ComposeApplication(this IServiceCollection services, string? environmentName)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            if (environmentName == "Development")
            {
                services.AddDbContext<EventStoreDbContext>(options =>
                    options.UseSqlServer(configuration["LOCAL_SQL_CONNECTIONSTRING"]));
            }
            else
            {
                services.AddDbContext<EventStoreDbContext>(options =>
                    options.UseSqlServer(configuration["AZURE_SQL_WRITE_CONNECTIONSTRING"]));
            }

            services.AddOptions<CloudAMQPSettings>().Configure<IConfiguration>((options, config) =>
            {
                config.GetSection(nameof(CloudAMQPSettings)).Bind(options);
            });

            //services.AddOptions<MediatRSettings>().Configure<IConfiguration>((options, config) =>
            //{
            //    config.GetSection(nameof(MediatRSettings)).Bind(options);
            //});

            //services.AddOptions<AzureSettings>().Configure<IConfiguration>((options, config) =>
            //{
            //    config.GetSection(nameof(AzureSettings)).Bind(options);
            //});

            

            // Register Class Library services
            services.RegisterInfrastructureServices();
            services.RegisterApplicationServices();

            // Auth Testing
            services.AddScoped<ITokenDecoder, TokenDecoder>();

            return services;
        }
    }
}
