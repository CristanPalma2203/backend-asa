using FluentValidation;

namespace backend_asa.Features.Lector.Listar;

public sealed class ListarLectoresValidator : AbstractValidator<ListarLectoresRequest>
{
    public ListarLectoresValidator()
    {
        RuleFor(x => x.Buscar)
            .MaximumLength(150)
            .WithMessage("La búsqueda no puede superar 150 caracteres.")
            .When(x => x.Buscar is not null);

        RuleFor(x => x.Estado)
            .IsInEnum()
            .WithMessage("El estado del lector no es válido.")
            .When(x => x.Estado is not null);
    }
}
