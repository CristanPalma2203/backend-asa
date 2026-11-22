using FluentValidation;

namespace backend_asa.Features.Luxia.Preguntar;

public sealed class PreguntarLuxiaValidator : AbstractValidator<PreguntarLuxiaRequest>
{
    public const int MaxPregunta = 500;
    public const int MaxMensajesHistorial = 10;
    public const int MaxTextoHistorial = 1000;

    public PreguntarLuxiaValidator()
    {
        RuleFor(x => x.Pregunta)
            .NotEmpty().WithMessage("Escribe una pregunta.")
            .MaximumLength(MaxPregunta).WithMessage($"La pregunta no puede superar {MaxPregunta} caracteres.");

        RuleFor(x => x.Historial)
            .Must(historial => historial!.Count <= MaxMensajesHistorial)
            .WithMessage($"El historial admite como máximo {MaxMensajesHistorial} mensajes.")
            .When(x => x.Historial is not null);

        RuleForEach(x => x.Historial).ChildRules(mensaje =>
        {
            mensaje.RuleFor(m => m.Rol)
                .Must(rol => rol is MensajeHistorial.RolUsuario or MensajeHistorial.RolLuxia)
                .WithMessage("El rol de cada mensaje debe ser \"usuario\" o \"luxia\".");

            mensaje.RuleFor(m => m.Texto)
                .NotEmpty().WithMessage("Los mensajes del historial no pueden estar vacíos.")
                .MaximumLength(MaxTextoHistorial).WithMessage($"Cada mensaje del historial admite hasta {MaxTextoHistorial} caracteres.");
        });
    }
}
