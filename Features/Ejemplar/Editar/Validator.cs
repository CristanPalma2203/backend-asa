using backend_asa.Features.Ejemplar.Crear;
using FluentValidation;

namespace backend_asa.Features.Ejemplar.Editar;

public sealed class EditarEjemplarValidator : AbstractValidator<EditarEjemplarRequest>
{
    public EditarEjemplarValidator()
    {
        RuleFor(x => CodigoInventario.Normalizar(x.CodigoInventario))
            .NotEmpty().WithMessage("El código de inventario es obligatorio.")
            .MaximumLength(CrearEjemplarValidator.CodigoMaxLength).WithMessage("El código de inventario no puede superar 30 caracteres.")
            .OverridePropertyName(nameof(EditarEjemplarRequest.CodigoInventario));

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("La versión del registro es obligatoria.")
            .Must(RowVersionToken.EsValido)
            .WithMessage("La versión del registro no es válida.");
    }
}
