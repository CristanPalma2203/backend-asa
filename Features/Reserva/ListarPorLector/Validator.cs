using FluentValidation;

namespace backend_asa.Features.Reserva.ListarPorLector;

public sealed class ListarReservasPorLectorValidator : AbstractValidator<ListarReservasPorLectorRequest>
{
    public ListarReservasPorLectorValidator()
    {
        RuleFor(x => x.LectorId)
            .GreaterThan(0)
            .WithMessage("El identificador del lector no es válido.");

        RuleFor(x => x.Estado)
            .IsInEnum()
            .WithMessage("El estado de la reserva no es válido.")
            .When(x => x.Estado is not null);
    }
}
