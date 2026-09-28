using PokemonCacheApi.Data;
using PokemonCacheApi.Middleware;
using PokemonCacheApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Pokemon Cache API",
        Version = "v1",
        Description = "Fetches Pokemon data from PokeAPI, caches it in SQL Server, and serves it back via a small REST API.",
    });
});

// Database access
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IPokemonRepository, PokemonRepository>();

// Business logic
builder.Services.AddScoped<IPokemonService, PokemonService>();

// Typed HttpClient for PokeAPI
var pokeApiBaseUrl = builder.Configuration["PokeApi:BaseUrl"] ?? "https://pokeapi.co/api/v2/";
var pokeApiTimeoutSeconds = builder.Configuration.GetValue<int?>("PokeApi:TimeoutSeconds") ?? 15;

builder.Services.AddHttpClient<IPokeApiClient, PokeApiClient>(client =>
{
    client.BaseAddress = new Uri(pokeApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(pokeApiTimeoutSeconds);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// Global error handling first, so it wraps everything below it.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Lightweight liveness check, handy for docker-compose healthchecks and quick smoke tests.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
