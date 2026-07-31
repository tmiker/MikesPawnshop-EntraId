using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Products.Shared.Abstractions;

namespace Products.Read.API.Abstractions
{
    public interface IProductMessageProcessor
    {
        Task<bool> ProcessProductMessageAsync(IProductMessage message);
        Task ProcessMessageRecordsFromQueue();
    }
}
