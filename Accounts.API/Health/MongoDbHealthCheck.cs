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

        public MongoDbHealthCheck(IConfiguration config)
        {
            _config = config;
            string? environment = _config["ASPNETCORE_ENVIRONMENT"];
            var client = environment == "Development" ? new MongoClient(_config["MongoSettings__MongoLocalConnection"]) : new MongoClient(_config["MongoSettings__AZURE_MONGO_CONNECTION"]);
            _database = client.GetDatabase(_config["MongoSettings__Database"]);
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
