using PokemonCacheApi.Models;

namespace PokemonCacheApi.Data;

public interface IPokemonRepository
{
    Task<PokemonRecord?> GetByIdAsync(int id, CancellationToken ct);
    Task<PokemonRecord?> GetByNameAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<PokemonRecord>> GetPageAsync(int limit, int offset, CancellationToken ct);
    Task<int> GetCountAsync(CancellationToken ct);

    /// <summary>
    /// Inserts a record. If a row with the same Id already exists (e.g. a concurrent
    /// request cached it first) the insert is silently skipped rather than throwing.
    /// </summary>
    Task InsertIfNotExistsAsync(PokemonRecord record, CancellationToken ct);
}
