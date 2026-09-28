using PokemonCacheApi.Data;
using PokemonCacheApi.Models;
using PokemonCacheApi.Models.Dtos;
using PokemonCacheApi.Models.External;

namespace PokemonCacheApi.Services;

/// <summary>
/// Implements the "check the database first, fall back to the third-party API,
/// then save and return" rule (assignment requirement #3) for both endpoints.
/// </summary>
public class PokemonService : IPokemonService
{
    private readonly IPokemonRepository _repository;
    private readonly IPokeApiClient _pokeApiClient;
    private readonly ILogger<PokemonService> _logger;

    public PokemonService(IPokemonRepository repository, IPokeApiClient pokeApiClient, ILogger<PokemonService> logger)
    {
        _repository = repository;
        _pokeApiClient = pokeApiClient;
        _logger = logger;
    }

    public async Task<PagedPokemonResponseDto> GetListAsync(int limit, int offset, CancellationToken ct)
    {
        var cachedCount = await _repository.GetCountAsync(ct);
        var source = "cache";

        // The cache starts empty, so there is nothing to page through yet.
        // Bootstrap it once by pulling the requested page straight from PokeAPI,
        // caching each record, and then serving the response from the same
        // "read from DB" code path used for every later call.
        if (cachedCount == 0)
        {
            _logger.LogInformation("Pokemon cache is empty; bootstrapping from PokeAPI (limit={Limit}, offset={Offset}).", limit, offset);
            var apiList = await _pokeApiClient.GetPokemonListAsync(limit, offset, ct);

            foreach (var item in apiList.Results)
            {
                var detail = await _pokeApiClient.GetPokemonAsync(item.Name, ct);
                if (detail is not null)
                {
                    await _repository.InsertIfNotExistsAsync(MapToRecord(detail), ct);
                }
            }

            source = "api";
        }

        var page = await _repository.GetPageAsync(limit, offset, ct);
        var totalCached = await _repository.GetCountAsync(ct);

        return new PagedPokemonResponseDto
        {
            Limit = limit,
            Offset = offset,
            TotalCached = totalCached,
            Source = source,
            Items = page.Select(record => MapToDto(record, source)).ToList(),
        };
    }

    public async Task<PokemonResponseDto?> GetByIdOrNameAsync(string idOrName, CancellationToken ct)
    {
        var normalized = idOrName.Trim().ToLowerInvariant();

        var cached = int.TryParse(normalized, out var id)
            ? await _repository.GetByIdAsync(id, ct)
            : await _repository.GetByNameAsync(normalized, ct);

        if (cached is not null)
        {
            return MapToDto(cached, "cache");
        }

        var external = await _pokeApiClient.GetPokemonAsync(normalized, ct);
        if (external is null)
        {
            return null;
        }

        var record = MapToRecord(external);
        await _repository.InsertIfNotExistsAsync(record, ct);

        return MapToDto(record, "api");
    }

    private static PokemonRecord MapToRecord(PokeApiPokemonResponse external) => new()
    {
        Id = external.Id,
        Name = external.Name,
        HeightDecimetres = external.Height,
        WeightHectograms = external.Weight,
        Types = string.Join(",", external.Types.OrderBy(t => t.Slot).Select(t => t.Type.Name)),
        ImageUrl = external.Sprites?.FrontDefault,
        CreatedAtUtc = DateTime.UtcNow,
    };

    private static PokemonResponseDto MapToDto(PokemonRecord record, string source) => new()
    {
        Id = record.Id,
        Name = record.Name,
        HeightDecimetres = record.HeightDecimetres,
        WeightHectograms = record.WeightHectograms,
        Types = string.IsNullOrWhiteSpace(record.Types)
            ? new List<string>()
            : record.Types.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
        ImageUrl = record.ImageUrl,
        Source = source,
    };
}
