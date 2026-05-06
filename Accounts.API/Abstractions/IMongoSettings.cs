namespace Accounts.API.Abstractions
{
    public interface IMongoSettings
    {
        string? MongoSettings__MongoLocalConnection { get; }
        string? MongoSettings__AZURE_MONGO_CONNECTION { get; }
        string? MongoSettings__Database { get; }
        string? MongoSettings__AccountCollection { get; }
    }
}
