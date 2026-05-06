using Accounts.API.Abstractions;

namespace Accounts.API.Infrastructure.Mongo
{
    public class MongoSettings : IMongoSettings
    {
        public string? MongoSettings__MongoLocalConnection { get; set; }
        public string? MongoSettings__AZURE_MONGO_CONNECTION { get; set; }
        public string? MongoSettings__Database { get; set; }
        public string? MongoSettings__AccountCollection { get; set; }
    }
}
