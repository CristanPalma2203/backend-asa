using FluentValidation;

namespace backend_asa.Features.Ejemplar.Desactivar;

public sealed class DesactivarEjemplarValidator : AbstractValidator<DesactivarEjemplarRequest>
{
    public DesactivarEjemplarValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del ejemplar no es válido.");
    }
}
