using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Infrastructure.Persistence;
using backend_asa.Infrastructure.Persistence.Seed;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpoints();
builder.Services.AddSingleton(TimeProvider.System);

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

builder.Services.AddDbContext<BibliotecaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("database-asa"))
        .UseSeeding((context, _) => DatosIniciales.Seed(context))
        .UseAsyncSeeding((context, _, cancellationToken) => DatosIniciales.SeedAsync(context, cancellationToken)));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
    await db.Database.MigrateAsync();
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("backend-asa API"));
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseCors("frontend");

app.MapHealthChecks("/health").ExcludeFromDescription();

app.MapEndpoints();

app.Run();
