using FluentValidation;
using Products.Write.Application.CQRS.DevTests;

namespace Products.Write.Application.Validators
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