using FluentValidation;

namespace backend_asa.Features.Libro.Obtener;

public sealed class ObtenerLibroValidator : AbstractValidator<ObtenerLibroRequest>
{
    public ObtenerLibroValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del libro no es válido.");
    }
}
