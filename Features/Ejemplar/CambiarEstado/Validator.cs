using FluentValidation;

namespace backend_asa.Features.Ejemplar.CambiarEstado;

public sealed class CambiarEstadoEjemplarValidator : AbstractValidator<CambiarEstadoEjemplarRequest>
{
    public CambiarEstadoEjemplarValidator()
    {
        RuleFor(x => x.Estado)
            .IsInEnum()
            .WithMessage("El estado del ejemplar no es válido.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("La versión del registro es obligatoria.")
            .Must(RowVersionToken.EsValido)
            .WithMessage("La versión del registro no es válida.");
    }
}
