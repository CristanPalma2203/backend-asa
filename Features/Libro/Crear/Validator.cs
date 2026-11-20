using FluentValidation;

namespace backend_asa.Features.Libro.Crear;

public sealed class CrearLibroValidator : AbstractValidator<CrearLibroRequest>
{
    public CrearLibroValidator()
    {
        RuleFor(x => x.Isbn)
            .NotEmpty().WithMessage("El ISBN es obligatorio.")
            .Must(LibroIsbn.EsValido).WithMessage("El ISBN debe ser ISBN-10 o ISBN-13 con dígito verificador válido.");

        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(200).WithMessage("El título no puede superar 200 caracteres.");

        RuleFor(x => x.Autor)
            .NotEmpty().WithMessage("El autor es obligatorio.")
            .MaximumLength(150).WithMessage("El autor no puede superar 150 caracteres.");

        RuleFor(x => x.Categoria)
            .NotEmpty().WithMessage("La categoría es obligatoria.")
            .MaximumLength(80).WithMessage("La categoría no puede superar 80 caracteres.");
    }
}
