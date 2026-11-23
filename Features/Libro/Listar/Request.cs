namespace backend_asa.Features.Libro.Listar;

public sealed record ListarLibrosRequest(
    string? Buscar,
    string? Search,
    string? Autor,
    string? Categoria,
    string? CategoriaId,
    int Page = 1,
    int PageSize = 10)
{
    public string? TextoBuscado => string.IsNullOrWhiteSpace(Buscar) ? Search : Buscar;
}
