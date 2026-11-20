using FluentValidation;

namespace backend_asa.Features.Lector.Editar;

public sealed class EditarLectorValidator : AbstractValidator<EditarLectorRequest>
{
    public EditarLectorValidator()
    {
        RuleFor(x => x.Identificador)
            .NotEmpty().WithMessage("El identificador es obligatorio.")
            .MaximumLength(20).WithMessage("El identificador no puede superar 20 caracteres.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(150).WithMessage("El correo no puede superar 150 caracteres.")
            .EmailAddress().WithMessage("El correo no es válido.");
    }
}
