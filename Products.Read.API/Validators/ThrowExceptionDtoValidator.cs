using FluentValidation;
using Products.Read.API.DTOs.DevTests;

namespace Products.Read.Validators
{
    public class ThrowExceptionDtoValidator : AbstractValidator<ThrowExceptionDTO>
    {
        public ThrowExceptionDtoValidator()
        {
            RuleFor(x => x.ExceptionType)
                .NotEmpty().WithMessage("An exception type must be provided.");
        }
    }
}