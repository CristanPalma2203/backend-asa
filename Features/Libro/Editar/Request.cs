namespace backend_asa.Features.Libro.Editar;

public sealed record EditarLibroRequest(string Isbn, string Titulo, string Autor, string Categoria);
