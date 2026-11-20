using FluentValidation;

namespace backend_asa.Features.Reserva.Cancelar;

public sealed class CancelarReservaValidator : AbstractValidator<CancelarReservaRequest>
{
    public CancelarReservaValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador de la reserva no es válido.");
    }
}
