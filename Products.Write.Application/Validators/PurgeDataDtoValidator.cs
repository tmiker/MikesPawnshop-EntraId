using FluentValidation;
using Products.Write.Application.DTOs;

namespace Products.Write.Application.Validators
{
    public class PurgeDataDtoValidator : AbstractValidator<PurgeDataDTO>
    {
        public PurgeDataDtoValidator()
        {
            RuleFor(x => x.PinNumber)
                .LessThanOrEqualTo(9999).GreaterThan(999).WithMessage("A valid 4-digit pin number must be provided.");
        }
    }
}