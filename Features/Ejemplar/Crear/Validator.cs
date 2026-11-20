using FluentValidation;

namespace backend_asa.Features.Ejemplar.Crear;

public sealed class CrearEjemplarValidator : AbstractValidator<CrearEjemplarRequest>
{
    public const int CodigoMaxLength = 30;

    public CrearEjemplarValidator()
    {
        RuleFor(x => CodigoInventario.Normalizar(x.CodigoInventario))
            .NotEmpty().WithMessage("El código de inventario es obligatorio.")
            .MaximumLength(CodigoMaxLength).WithMessage("El código de inventario no puede superar 30 caracteres.")
            .OverridePropertyName(nameof(CrearEjemplarRequest.CodigoInventario));
    }
}
