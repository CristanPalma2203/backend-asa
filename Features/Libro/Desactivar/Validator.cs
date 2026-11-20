using FluentValidation;

namespace backend_asa.Features.Libro.Desactivar;

public sealed class DesactivarLibroValidator : AbstractValidator<DesactivarLibroRequest>
{
    public DesactivarLibroValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del libro no es válido.");
    }
}
