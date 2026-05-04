namespace Accounts.API.Abstractions
{
    public interface IMongoSettings
    {
        string? MongoLocalConnection { get; }
        string? AZURE_MONGO_CONNECTION { get; }
        string? Database { get; }
        string? AccountCollection { get; }
    }
}
