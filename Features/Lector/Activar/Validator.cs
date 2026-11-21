using FluentValidation;

namespace backend_asa.Features.Lector.Activar;

public sealed class ActivarLectorValidator : AbstractValidator<ActivarLectorRequest>
{
    public ActivarLectorValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del lector no es válido.");
    }
}
