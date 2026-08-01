using FluentValidation;
using Products.Write.Application.DTOs;

namespace Products.Write.Application.Validators
{
    public class DeleteImageDtoValidator : AbstractValidator<DeleteImageDTO>
    {
        public DeleteImageDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("A product Id must be provided.");
            RuleFor(x => x.FileName)
                .NotEmpty().Length(0, 255).WithMessage($"A file name must be provided.");
        }
    }
}