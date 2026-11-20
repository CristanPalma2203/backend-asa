using FluentValidation;

namespace backend_asa.Features.Lector.Obtener;

public sealed class ObtenerLectorValidator : AbstractValidator<ObtenerLectorRequest>
{
    public ObtenerLectorValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del lector no es válido.");
    }
}
