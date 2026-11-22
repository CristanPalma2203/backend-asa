using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.OpenApi;
using backend_asa.Domain;
using backend_asa.Features.Auth;
using backend_asa.Features.Luxia;
using backend_asa.Infrastructure.Persistence;
using backend_asa.Infrastructure.Persistence.Seed;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(builder.Environment.IsDevelopment()
        ? "Jwt:Key está vacía. Configúrala en appsettings.Development.json con al menos 32 caracteres."
        : "Jwt:Key está vacía. Fuera de Development la aplicación no arranca sin una clave JWT.");
}

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");
}

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddSingleton<PasswordHasher<Usuario>>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role",
            NameClaimType = "sub",
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await Results.Problem(
                    title: "No autenticado",
                    detail: "Debes iniciar sesión.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    extensions: new Dictionary<string, object?> { ["code"] = "no-autenticado" })
                    .ExecuteAsync(context.HttpContext);
            },
            OnForbidden = async context =>
            {
                await Results.Problem(
                    title: "Acceso denegado",
                    detail: "No tienes permiso para esta operación.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: new Dictionary<string, object?> { ["code"] = "acceso-denegado" })
                    .ExecuteAsync(context.HttpContext);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddSchemaTransformer<EnumComoStringSchemaTransformer>();
});
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpoints();
builder.Services.AddSingleton(TimeProvider.System);

var luxiaSection = builder.Configuration.GetSection(LuxiaOptions.SectionName);
builder.Services.Configure<LuxiaOptions>(luxiaSection);
builder.Services.AddHttpClient<MoonshotClient>((services, http) =>
    http.BaseAddress = new Uri(services.GetRequiredService<IOptions<LuxiaOptions>>().Value.BaseUrl));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
        await Results.Problem(
            title: "Demasiadas solicitudes",
            detail: "Hiciste muchas preguntas seguidas. Espera un momento y vuelve a intentarlo.",
            statusCode: StatusCodes.Status429TooManyRequests,
            extensions: new Dictionary<string, object?> { ["code"] = "demasiadas-solicitudes" })
            .ExecuteAsync(context.HttpContext);
    options.AddPolicy(LuxiaOptions.PoliticaPorUsuario, http =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.User.Identity?.Name ?? string.Empty,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = http.RequestServices.GetRequiredService<IOptions<LuxiaOptions>>().Value.PreguntasPorMinuto,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BibliotecaDbContext>();

var adminPassword = builder.Configuration["Seed:AdminPassword"];
var lectorPassword = builder.Configuration["Seed:LectorPassword"];

builder.Services.AddDbContext<BibliotecaDbContext>(options =>
    options.UseSqlServer(
            builder.Configuration.GetConnectionString("database-asa"),
            sql => sql.EnableRetryOnFailure())
        .UseSeeding((context, _) => DatosIniciales.Seed(context, adminPassword, lectorPassword))
        .UseAsyncSeeding((context, _, cancellationToken) => DatosIniciales.SeedAsync(context, adminPassword, lectorPassword, cancellationToken)));

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();

var migrarAlArrancar = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Database:MigrateOnStartup");

if (migrarAlArrancar)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Docs:Enabled"))
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(o => o.WithTitle("backend-asa API")).AllowAnonymous();
    app.MapGet("/", () => Results.Redirect("/scalar")).AllowAnonymous().ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health").AllowAnonymous().ExcludeFromDescription();

app.MapEndpoints();

app.Run();

public partial class Program { }
