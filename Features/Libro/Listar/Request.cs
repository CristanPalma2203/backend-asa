
namespace backend_asa.Features.Libro.Listar;

public sealed record ListarLibrosRequest(
    string? Buscar,
    string? Search,
    string? Autor,
    string? Categoria,
    int Page = 1,
    int PageSize = 10)
{
    // `search` es un alias de `buscar`; si llegan los dos, manda `buscar`.
    public string? TextoBuscado => string.IsNullOrWhiteSpace(Buscar) ? Search : Buscar;
}
