namespace backend_asa.Features.Libro.Crear;

public sealed record CrearLibroResponse(int IdLibro, string Isbn, string Titulo, string Autor, string Categoria);
