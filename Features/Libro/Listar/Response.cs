namespace backend_asa.Features.Libro.Listar;

public sealed record ListarLibrosItem(
    int IdLibro,
    string Isbn,
    string Titulo,
    string Autor,
    string Categoria,
    int TotalEjemplares,
    int Disponibles);
