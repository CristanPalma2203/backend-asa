using FluentValidation;

namespace backend_asa.Features.Libro.Listar;

public sealed class ListarLibrosValidator : AbstractValidator<ListarLibrosRequest>
{
    public ListarLibrosValidator()
    {
        RuleFor(x => x.Buscar)
            .MaximumLength(200)
            .WithMessage("La búsqueda no puede superar 200 caracteres.")
            .When(x => x.Buscar is not null);

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .WithMessage("La búsqueda no puede superar 200 caracteres.")
            .When(x => x.Search is not null);

        RuleFor(x => x.Autor)
            .MaximumLength(150)
            .WithMessage("El autor no puede superar 150 caracteres.")
            .When(x => x.Autor is not null);

        RuleFor(x => x.Categoria)
            .MaximumLength(100)
            .WithMessage("La categoría no puede superar 100 caracteres.")
            .When(x => x.Categoria is not null);

        RuleFor(x => x.CategoriaId)
            .MaximumLength(80)
            .WithMessage("La categoría no puede superar 80 caracteres.")
            .When(x => x.CategoriaId is not null);
    }
}
