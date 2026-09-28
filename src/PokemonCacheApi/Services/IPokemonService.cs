using PokemonCacheApi.Models.Dtos;

namespace PokemonCacheApi.Services;

public interface IPokemonService
{
    Task<PagedPokemonResponseDto> GetListAsync(int limit, int offset, CancellationToken ct);

    /// <summary>Returns null if the Pokemon does not exist in the cache nor in PokeAPI.</summary>
    Task<PokemonResponseDto?> GetByIdOrNameAsync(string idOrName, CancellationToken ct);
}
