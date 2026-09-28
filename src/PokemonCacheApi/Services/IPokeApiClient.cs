using PokemonCacheApi.Models.External;

namespace PokemonCacheApi.Services;

public interface IPokeApiClient
{
    /// <summary>Fetches one Pokemon by id or name. Returns null if PokeAPI responds 404.</summary>
    Task<PokeApiPokemonResponse?> GetPokemonAsync(string idOrName, CancellationToken ct);

    /// <summary>Fetches a page of the "index" of Pokemon names/urls (not full detail).</summary>
    Task<PokeApiListResponse> GetPokemonListAsync(int limit, int offset, CancellationToken ct);
}
