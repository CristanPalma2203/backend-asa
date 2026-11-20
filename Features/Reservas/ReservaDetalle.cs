using backend_asa.Domain.Enums;

namespace backend_asa.Features.Reservas;

public sealed record ReservaDetalle(
    int IdReserva,
    int IdLibro,
    string Titulo,
    string CodigoInventario,
    EstadoReserva Estado,
    DateTime FechaSolicitud,
    DateTime? FechaCancelacion);
