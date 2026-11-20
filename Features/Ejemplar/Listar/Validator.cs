using FluentValidation;

namespace backend_asa.Features.Ejemplar.Listar;

public sealed class ListarEjemplaresValidator : AbstractValidator<ListarEjemplaresRequest>
{
    public ListarEjemplaresValidator()
    {
        RuleFor(x => x.IdLibro)
            .GreaterThan(0)
            .WithMessage("El identificador del libro no es válido.");
    }
}
