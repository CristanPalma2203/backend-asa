namespace backend_asa.Features.Libro.Crear;

public sealed record CrearLibroRequest(string Isbn, string Titulo, string Autor, string Categoria);
