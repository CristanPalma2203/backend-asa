using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using backend_asa.Features.Luxia;
using backend_asa.Features.Luxia.Preguntar;
using backend_asa.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend_asa.Tests;

public sealed class LuxiaTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    private const string PrincipitoIsbn = "9788498381498";

    [Fact]
    public async Task Preguntar_ConLibrosReales_RespondeConElStockDeLosEjemplaresDisponibles()
    {
        var moonshot = new MoonshotFalso(
            LlamadaABuscarLibros("""{"search":"Principito"}"""),
            TextoFinal("Sí, El Principito tiene 3 ejemplares disponibles. Ábrelo en su ficha para reservarlo."));
        var idPrincipito = await Fixture.EnDb(db => db.Libros
            .Where(libro => libro.Isbn == PrincipitoIsbn)
            .Select(libro => libro.IdLibro)
            .SingleAsync());

        var resultado = await Preguntar(moonshot, "¿Tienen El Principito?");

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.Texto.Should().Contain("El Principito");
        resultado.Value.IdsLibros.Should().Equal(idPrincipito);
        var herramienta = moonshot.MensajeDeHerramientaEnviado(1);
        herramienta.Should().Contain("\"titulo\":\"El Principito\"").And.Contain("\"ejemplares_disponibles\":3");
    }

    [Fact]
    public async Task Preguntar_HerramientaSinResultados_SeLoDiceAlModeloYNoDevuelveLibros()
    {
        var moonshot = new MoonshotFalso(
            LlamadaABuscarLibros("""{"search":"Libro que no existe"}"""),
            TextoFinal("No encontré ese libro en el catálogo."));

        var resultado = await Preguntar(moonshot, "¿Tienen Libro que no existe?");

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.IdsLibros.Should().BeEmpty();
        moonshot.MensajeDeHerramientaEnviado(1).Should().StartWith("Sin resultados");
    }

    [Fact]
    public async Task Preguntar_SinLlave_Devuelve503SinLlamarAMoonshot()
    {
        var moonshot = new MoonshotFalso(TextoFinal("No debería llegar aquí."));

        var resultado = await Preguntar(moonshot, "¿Qué novelas hay?", apiKey: null);

        resultado.IsSuccess.Should().BeFalse();
        resultado.Error!.Code.Should().Be("luxia-no-disponible");
        moonshot.Solicitudes.Should().BeEmpty();
    }

    [Fact]
    public async Task Preguntar_MoonshotFalla_Devuelve503()
    {
        var moonshot = new MoonshotFalso(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var resultado = await Preguntar(moonshot, "¿Qué novelas hay?");

        resultado.Error!.Code.Should().Be("luxia-no-disponible");
    }

    [Fact]
    public async Task Preguntar_MoonshotNoContestaATiempo_Devuelve503()
    {
        var moonshot = new MoonshotFalso { Demora = Timeout.InfiniteTimeSpan };

        var resultado = await Preguntar(moonshot, "¿Qué novelas hay?", tiempoMaximo: TimeSpan.FromMilliseconds(200));

        resultado.Error!.Code.Should().Be("luxia-no-disponible");
    }

    [Fact]
    public async Task Endpoint_SinToken_Devuelve401()
    {
        var response = await Client.PostAsJsonAsync("/api/luxia/preguntar", new { pregunta = "¿Qué novelas hay?" }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Endpoint_SinLlaveConfigurada_Devuelve503ConCodigoParaElRespaldo()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/luxia/preguntar", new { pregunta = "¿Qué novelas hay?" }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await Codigo(response)).Should().Be("luxia-no-disponible");
    }

    [Fact]
    public async Task Endpoint_MasPreguntasQueElLimitePorMinuto_Devuelve429()
    {
        var ana = await LoginAsync(AnaCorreo, AnaPassword);
        var limite = new LuxiaOptions().PreguntasPorMinuto;

        var respuestas = new List<HttpResponseMessage>();
        for (var i = 0; i <= limite; i++)
        {
            respuestas.Add(await ana.PostAsJsonAsync("/api/luxia/preguntar", new { pregunta = "¿Qué novelas hay?" }, Json));
        }

        respuestas.Take(limite).Should().OnlyContain(r => r.StatusCode == HttpStatusCode.ServiceUnavailable);
        respuestas.Last().StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await Codigo(respuestas.Last())).Should().Be("demasiadas-solicitudes");
    }

    private async Task<Common.Results.Result<PreguntarLuxiaResponse>> Preguntar(
        MoonshotFalso moonshot,
        string pregunta,
        string? apiKey = "llave-de-prueba",
        TimeSpan? tiempoMaximo = null)
    {
        var opciones = Options.Create(new LuxiaOptions
        {
            BaseUrl = "https://moonshot.test/v1/",
            Modelo = "kimi-test",
            ApiKey = apiKey,
            TiempoMaximo = tiempoMaximo ?? TimeSpan.FromSeconds(15)
        });
        var cliente = new MoonshotClient(
            new HttpClient(moonshot) { BaseAddress = new Uri(opciones.Value.BaseUrl) },
            opciones);

        return await Fixture.EnDb((BibliotecaDbContext db) => PreguntarLuxiaHandler.Handle(
            new PreguntarLuxiaRequest(pregunta, [new MensajeHistorial(MensajeHistorial.RolUsuario, "Hola")]),
            cliente,
            opciones,
            db,
            CancellationToken.None));
    }

    private static HttpResponseMessage LlamadaABuscarLibros(string argumentos) => Respuesta(new
    {
        choices = new[]
        {
            new
            {
                message = new
                {
                    role = "assistant",
                    content = "",
                    tool_calls = new[]
                    {
                        new { id = "llamada-1", type = "function", function = new { name = "buscar_libros", arguments = argumentos } }
                    }
                }
            }
        }
    });

    private static HttpResponseMessage TextoFinal(string texto) => Respuesta(new
    {
        choices = new[] { new { message = new { role = "assistant", content = texto } } }
    });

    private static HttpResponseMessage Respuesta(object cuerpo) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json")
    };

    private sealed class MoonshotFalso(params HttpResponseMessage[] respuestas) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _pendientes = new(respuestas);

        public List<JsonElement> Solicitudes { get; } = [];

        public TimeSpan Demora { get; init; } = TimeSpan.Zero;

        public string MensajeDeHerramientaEnviado(int indiceSolicitud) =>
            Solicitudes[indiceSolicitud].GetProperty("messages").EnumerateArray()
                .Single(mensaje => mensaje.GetProperty("role").GetString() == "tool")
                .GetProperty("content").GetString()!;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Demora, cancellationToken);
            Solicitudes.Add(JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken)).RootElement);
            return _pendientes.Dequeue();
        }
    }
}
