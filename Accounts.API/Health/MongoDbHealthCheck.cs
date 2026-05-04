using Accounts.API.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Accounts.API.Health
{
    public class MongoDbHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _config;
        private readonly IMongoDatabase _database;

        public MongoDbHealthCheck(IConfiguration config, IMongoSettings mongoSettings)
        {
            _config = config;
            string? environment = _config["ASPNETCORE_ENVIRONMENT"];
            var client = environment == "Development" ? new MongoClient(mongoSettings.MongoLocalConnection) : new MongoClient(mongoSettings.AZURE_MONGO_CONNECTION);
            _database = client.GetDatabase(mongoSettings.Database);
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                return HealthCheckResult.Healthy("MongoDB is healthy");
            }
            catch
            {
                return HealthCheckResult.Unhealthy("MongoDB is unreachable");
            }
        }
    }
}
