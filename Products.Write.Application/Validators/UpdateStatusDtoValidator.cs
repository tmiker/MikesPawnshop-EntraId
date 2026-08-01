using FluentValidation;
using Products.Write.Application.DTOs;

namespace Products.Write.Application.Validators
{
    public class UpdateStatusDtoValidator : AbstractValidator<UpdateStatusDTO>
    {
        public UpdateStatusDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotNull().WithMessage("A product Id must be provided.");
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage($"A status must be provided.");
        }
    }
}