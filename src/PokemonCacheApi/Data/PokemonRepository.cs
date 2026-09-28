using Microsoft.Data.SqlClient;
using PokemonCacheApi.Models;

namespace PokemonCacheApi.Data;

/// <summary>
/// Data access for the Pokemon cache table. Deliberately uses plain SQL via
/// Microsoft.Data.SqlClient (per assignment requirement #5 — no ORM) instead
/// of Entity Framework or Dapper.
/// </summary>
public class PokemonRepository : IPokemonRepository
{
    private const int SqlUniqueViolation = 2627;
    private const int SqlDuplicateKey = 2601;

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PokemonRepository> _logger;

    public PokemonRepository(IDbConnectionFactory connectionFactory, ILogger<PokemonRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<PokemonRecord?> GetByIdAsync(int id, CancellationToken ct)
    {
        const string sql = @"
            SELECT Id, Name, HeightDecimetres, WeightHectograms, Types, ImageUrl, CreatedAtUtc
            FROM dbo.Pokemon
            WHERE Id = @Id;";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return Map(reader);
        }

        return null;
    }

    public async Task<PokemonRecord?> GetByNameAsync(string name, CancellationToken ct)
    {
        const string sql = @"
            SELECT Id, Name, HeightDecimetres, WeightHectograms, Types, ImageUrl, CreatedAtUtc
            FROM dbo.Pokemon
            WHERE Name = @Name;";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Name", name);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return Map(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<PokemonRecord>> GetPageAsync(int limit, int offset, CancellationToken ct)
    {
        const string sql = @"
            SELECT Id, Name, HeightDecimetres, WeightHectograms, Types, ImageUrl, CreatedAtUtc
            FROM dbo.Pokemon
            ORDER BY Id
            OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;";

        var results = new List<PokemonRecord>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Offset", offset);
        command.Parameters.AddWithValue("@Limit", limit);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public async Task<int> GetCountAsync(CancellationToken ct)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.Pokemon;";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(ct);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

    public async Task InsertIfNotExistsAsync(PokemonRecord record, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO dbo.Pokemon (Id, Name, HeightDecimetres, WeightHectograms, Types, ImageUrl, CreatedAtUtc)
            VALUES (@Id, @Name, @HeightDecimetres, @WeightHectograms, @Types, @ImageUrl, @CreatedAtUtc);";

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", record.Id);
        command.Parameters.AddWithValue("@Name", record.Name);
        command.Parameters.AddWithValue("@HeightDecimetres", (object?)record.HeightDecimetres ?? DBNull.Value);
        command.Parameters.AddWithValue("@WeightHectograms", (object?)record.WeightHectograms ?? DBNull.Value);
        command.Parameters.AddWithValue("@Types", (object?)record.Types ?? DBNull.Value);
        command.Parameters.AddWithValue("@ImageUrl", (object?)record.ImageUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAtUtc", record.CreatedAtUtc);

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex) when (ex.Number is SqlUniqueViolation or SqlDuplicateKey)
        {
            // Another request already cached this Pokemon between our cache-miss check
            // and this insert. That's fine — the data is there, so just log and move on.
            _logger.LogInformation("Pokemon {Id} was already cached by a concurrent request.", record.Id);
        }
    }

    private static PokemonRecord Map(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        HeightDecimetres = reader.IsDBNull(reader.GetOrdinal("HeightDecimetres")) ? null : reader.GetInt32(reader.GetOrdinal("HeightDecimetres")),
        WeightHectograms = reader.IsDBNull(reader.GetOrdinal("WeightHectograms")) ? null : reader.GetInt32(reader.GetOrdinal("WeightHectograms")),
        Types = reader.IsDBNull(reader.GetOrdinal("Types")) ? null : reader.GetString(reader.GetOrdinal("Types")),
        ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
    };
}
