using backend_asa.Domain;
using backend_asa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Infrastructure.Persistence.Seed;

public static class DatosIniciales
{
    private sealed record CopiaSeed(string Codigo, EstadoEjemplar Estado);

    private sealed record LibroSeed(
        string Isbn,
        string Titulo,
        string Autor,
        string Categoria,
        EstadoLibro Estado,
        CopiaSeed[] Copias);

    private sealed record LectorSeed(
        string Identificador,
        string Nombre,
        string Correo,
        EstadoLector Estado);

    private static readonly LibroSeed[] Libros =
    [
        Libro("9788498381498", "El Principito", "Antoine de Saint-Exupéry", "Infantil", EstadoLibro.Activo, 1, 3, EstadoEjemplar.Disponible),
        Libro("9780307474728", "Cien años de soledad", "Gabriel García Márquez", "Novela", EstadoLibro.Activo, 4, 2, EstadoEjemplar.Disponible),
        Libro("9788420471839", "Pedro Páramo", "Juan Rulfo", "Novela", EstadoLibro.Activo, 6, 1, EstadoEjemplar.Disponible),
        Libro("9788437604572", "Rayuela", "Julio Cortázar", "Novela", EstadoLibro.Activo, 7, 3, EstadoEjemplar.Disponible),
        Libro("9788401242359", "La casa de los espíritus", "Isabel Allende", "Novela", EstadoLibro.Activo, 10, 2, EstadoEjemplar.Disponible),
        Libro("9780061120084", "Matar a un ruiseñor", "Harper Lee", "Novela", EstadoLibro.Activo, 12, 1, EstadoEjemplar.Disponible),
        Libro("9780451524935", "1984", "George Orwell", "Novela", EstadoLibro.Activo, 13, 3, EstadoEjemplar.Disponible),
        Libro("9781451673319", "Fahrenheit 451", "Ray Bradbury", "Novela", EstadoLibro.Activo, 16, 2, EstadoEjemplar.Disponible),
        Libro("9788420412146", "Don Quijote de la Mancha", "Miguel de Cervantes", "Clásicos", EstadoLibro.Activo, 18, 2, EstadoEjemplar.Inactivo),
        Libro("9780141439518", "Orgullo y prejuicio", "Jane Austen", "Clásicos", EstadoLibro.Activo, 20, 1, EstadoEjemplar.Disponible),
        Libro("9780143058144", "Crimen y castigo", "Fiódor Dostoievski", "Clásicos", EstadoLibro.Activo, 21, 3, EstadoEjemplar.Disponible),
        Libro("9780140268867", "La Odisea", "Homero", "Clásicos", EstadoLibro.Activo, 24, 2, EstadoEjemplar.Disponible),
        Libro("9780743477123", "Hamlet", "William Shakespeare", "Clásicos", EstadoLibro.Activo, 26, 1, EstadoEjemplar.Disponible),
        Libro("9780547928227", "El hobbit", "J. R. R. Tolkien", "Clásicos", EstadoLibro.Activo, 27, 3, EstadoEjemplar.Disponible),
        Libro("9780062316097", "Sapiens", "Yuval Noah Harari", "Historia", EstadoLibro.Activo, 30, 2, EstadoEjemplar.Disponible),
        Libro("9780062464316", "Homo Deus", "Yuval Noah Harari", "Historia", EstadoLibro.Activo, 32, 1, EstadoEjemplar.Disponible),
        Libro("9780393317558", "Guns, Germs, and Steel", "Jared Diamond", "Historia", EstadoLibro.Activo, 33, 2, EstadoEjemplar.Disponible),
        Libro("9780553380163", "Breve historia del tiempo", "Stephen Hawking", "Ciencia", EstadoLibro.Activo, 35, 3, EstadoEjemplar.Disponible),
        Libro("9780345539434", "Cosmos", "Carl Sagan", "Ciencia", EstadoLibro.Activo, 38, 2, EstadoEjemplar.Disponible),
        Libro("9780198788607", "El gen egoísta", "Richard Dawkins", "Ciencia", EstadoLibro.Activo, 40, 1, EstadoEjemplar.Disponible),
        Libro("9780142410318", "Charlie y la fábrica de chocolate", "Roald Dahl", "Infantil", EstadoLibro.Activo, 41, 2, EstadoEjemplar.Disponible),
        Libro("9788478884452", "Harry Potter y la piedra filosofal", "J. K. Rowling", "Infantil", EstadoLibro.Activo, 43, 3, EstadoEjemplar.Disponible),
        Libro("9780132350884", "Clean Code", "Robert C. Martin", "Tecnología", EstadoLibro.Activo, 46, 2, EstadoEjemplar.Disponible),
        Libro("9780135957059", "The Pragmatic Programmer", "David Thomas, Andrew Hunt", "Tecnología", EstadoLibro.Activo, 48, 1, EstadoEjemplar.Disponible),
        Libro("9780201633610", "Design Patterns", "Erich Gamma, Richard Helm, Ralph Johnson, John Vlissides", "Tecnología", EstadoLibro.Activo, 49, 3, EstadoEjemplar.Disponible)
    ];

    private static readonly LectorSeed[] Lectores =
    [
        new("LEC-ANA-001", "Ana López", "ana.lopez@demo.local", EstadoLector.Activo),
        new("LEC-CARLOS-001", "Carlos Méndez", "carlos.mendez@demo.local", EstadoLector.Inactivo)
    ];

    public static void Seed(DbContext context)
    {
        var libros = context.Set<Libro>().ToDictionary(l => l.Isbn);
        var codigos = context.Set<Ejemplar>().Select(e => e.CodigoInventario).ToHashSet();
        var correos = context.Set<Lector>().Select(l => l.Correo).ToHashSet();
        AgregarFaltantes(context, libros, codigos, correos);
        context.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var libros = await context.Set<Libro>().ToDictionaryAsync(l => l.Isbn, cancellationToken);
        var codigos = (await context.Set<Ejemplar>().Select(e => e.CodigoInventario).ToListAsync(cancellationToken)).ToHashSet();
        var correos = (await context.Set<Lector>().Select(l => l.Correo).ToListAsync(cancellationToken)).ToHashSet();
        AgregarFaltantes(context, libros, codigos, correos);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void AgregarFaltantes(
        DbContext context,
        Dictionary<string, Libro> libros,
        HashSet<string> codigos,
        HashSet<string> correos)
    {
        var ahora = DateTime.UtcNow;

        foreach (var item in Libros)
        {
            if (!libros.TryGetValue(item.Isbn, out var libro))
            {
                libro = new Libro
                {
                    Isbn = item.Isbn,
                    Titulo = item.Titulo,
                    Autor = item.Autor,
                    Categoria = item.Categoria,
                    Estado = item.Estado
                };
                context.Add(libro);
                libros[item.Isbn] = libro;
            }

            foreach (var copia in item.Copias)
            {
                if (!codigos.Add(copia.Codigo))
                {
                    continue;
                }

                context.Add(new Ejemplar
                {
                    Libro = libro,
                    CodigoInventario = copia.Codigo,
                    Estado = copia.Estado,
                    CreatedAt = ahora,
                    UpdatedAt = ahora
                });
            }
        }

        foreach (var item in Lectores)
        {
            if (!correos.Add(item.Correo))
            {
                continue;
            }

            context.Add(new Lector
            {
                Identificador = item.Identificador,
                Nombre = item.Nombre,
                Correo = item.Correo,
                Estado = item.Estado
            });
        }
    }

    private static LibroSeed Libro(
        string isbn,
        string titulo,
        string autor,
        string categoria,
        EstadoLibro estado,
        int desde,
        int cantidad,
        EstadoEjemplar estadoCopia)
    {
        var copias = new CopiaSeed[cantidad];
        for (var i = 0; i < cantidad; i++)
        {
            copias[i] = new CopiaSeed($"BIB-{(desde + i):000}", estadoCopia);
        }

        return new LibroSeed(isbn, titulo, autor, categoria, estado, copias);
    }
}
