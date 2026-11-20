using FluentValidation;

namespace backend_asa.Features.Reserva.Obtener;

public sealed class ObtenerReservaValidator : AbstractValidator<ObtenerReservaRequest>
{
    public ObtenerReservaValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador de la reserva no es válido.");
    }
}
