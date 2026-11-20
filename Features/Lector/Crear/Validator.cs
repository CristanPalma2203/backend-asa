using FluentValidation;

namespace backend_asa.Features.Lector.Crear;

public sealed class CrearLectorValidator : AbstractValidator<CrearLectorRequest>
{
    public CrearLectorValidator()
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

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Password));
    }
}
