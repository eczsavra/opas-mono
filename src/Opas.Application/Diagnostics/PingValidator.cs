using FluentValidation;

namespace Opas.Application.Diagnostics;

public sealed class PingValidator : AbstractValidator<PingQuery>
{
    public PingValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("name is required.")
            .MaximumLength(50).WithMessage("name must be <= 50 chars.");
    }
}
