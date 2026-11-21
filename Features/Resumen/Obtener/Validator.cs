using FluentValidation;

namespace backend_asa.Features.Resumen.Obtener;

public sealed class ObtenerResumenValidator : AbstractValidator<ObtenerResumenRequest>
{
    public ObtenerResumenValidator()
    {
        RuleFor(x => x.Dias)
            .Must(dias => dias is 14 or 30)
            .WithMessage("dias debe ser 14 o 30.");
    }
}
