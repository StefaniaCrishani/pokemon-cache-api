using Microsoft.Data.SqlClient;

namespace PokemonCacheApi.Data;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        // Reads from appsettings.json "ConnectionStrings:PokemonDb", which can be
        // overridden at runtime with the environment variable
        // ConnectionStrings__PokemonDb (standard ASP.NET Core config convention).
        _connectionString = configuration.GetConnectionString("PokemonDb")
            ?? throw new InvalidOperationException(
                "Connection string 'PokemonDb' is not configured. Set it in appsettings.json " +
                "or via the ConnectionStrings__PokemonDb environment variable.");
    }

    public SqlConnection CreateConnection() => new SqlConnection(_connectionString);
}
