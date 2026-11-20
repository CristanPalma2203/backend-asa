using FluentValidation;

namespace backend_asa.Features.Lector.Desactivar;

public sealed class DesactivarLectorValidator : AbstractValidator<DesactivarLectorRequest>
{
    public DesactivarLectorValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del lector no es válido.");
    }
}
