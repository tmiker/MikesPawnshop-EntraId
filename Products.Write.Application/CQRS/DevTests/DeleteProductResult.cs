namespace Products.Write.Application.CQRS.DevTests
{
    public class DeleteProductResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public DeleteProductResult(bool isSuccess, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }
    }
}
