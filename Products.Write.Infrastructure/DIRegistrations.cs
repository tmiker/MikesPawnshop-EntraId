using Microsoft.Extensions.DependencyInjection;
using Products.Write.Infrastructure.Abstractions;
using Products.Write.Infrastructure.EventStores;
using Products.Write.Infrastructure.Repositories;

namespace Products.Write.Infrastructure
{
    public static class DIRegistrations
    {
        public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services)
        {
            // Register Product Event Store
            services.AddScoped<IProductEventStore, ProductEventStore>();
            // Register Product Repository
            services.AddScoped<IProductRepository, ProductRepository>();

            return services;
        }
    }
}
