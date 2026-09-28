using Microsoft.Data.SqlClient;

namespace PokemonCacheApi.Data;

public interface IDbConnectionFactory
{
    /// <summary>Creates a new, unopened SqlConnection built from the configured connection string.</summary>
    SqlConnection CreateConnection();
}
