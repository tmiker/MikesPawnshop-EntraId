using Consumer.Blazor.Client.DTOs.Products.Test;

namespace Consumer.Blazor.Client.Abstractions
{
    public interface IProductsReadHttpService
    {
        Task<(bool IsSuccess, string? ErrorMessage)> ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken);
    }
}
