using backend_asa.Domain.Enums;

namespace backend_asa.Features.Reserva.Crear;

public sealed record CrearReservaResponse(
    int IdReserva,
    int IdEjemplar,
    string CodigoInventario,
    EstadoReserva Estado,
    DateTime FechaSolicitud);
