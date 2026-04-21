using Accounts.API.Abstractions;

namespace Accounts.API.Infrastructure.Mongo
{
    public class MongoSettings : IMongoSettings
    {
        public string? MongoLocalConnection { get; set; }
        public string? MongoAzureConnection { get; set; }
        public string? Database { get; set; }
        public string? AccountCollection { get; set; }
    }
}
