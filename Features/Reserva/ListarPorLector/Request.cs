using backend_asa.Domain.Enums;

namespace backend_asa.Features.Reserva.ListarPorLector;

public sealed record ListarReservasPorLectorRequest(
    int LectorId,
    EstadoReserva? Estado,
    int Page = 1,
    int PageSize = 10);
