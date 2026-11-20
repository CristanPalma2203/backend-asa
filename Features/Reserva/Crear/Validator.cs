using FluentValidation;

namespace backend_asa.Features.Reserva.Crear;

public sealed class CrearReservaValidator : AbstractValidator<CrearReservaRequest>
{
    public CrearReservaValidator()
    {
        RuleFor(x => x.LectorId)
            .GreaterThan(0)
            .WithMessage("El identificador del lector no es válido.");

        RuleFor(x => x.LibroId)
            .GreaterThan(0)
            .WithMessage("El identificador del libro no es válido.");
    }
}
