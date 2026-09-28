using System.Net;
using System.Net.Http.Json;
using PokemonCacheApi.Exceptions;
using PokemonCacheApi.Models.External;

namespace PokemonCacheApi.Services;

/// <summary>
/// Thin wrapper around the public PokeAPI (https://pokeapi.co). PokeAPI is free,
/// requires no API key, and offers both a paged list endpoint and a per-record
/// lookup by id or name, which maps cleanly onto the two required endpoints.
/// </summary>
public class PokeApiClient : IPokeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PokeApiClient> _logger;

    public PokeApiClient(HttpClient httpClient, ILogger<PokeApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PokeApiPokemonResponse?> GetPokemonAsync(string idOrName, CancellationToken ct)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"pokemon/{Uri.EscapeDataString(idOrName)}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PokeApiPokemonResponse>(cancellationToken: ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "PokeAPI request failed for '{IdOrName}'.", idOrName);
            throw new UpstreamApiException($"Could not reach PokeAPI while looking up '{idOrName}'.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "PokeAPI request timed out for '{IdOrName}'.", idOrName);
            throw new UpstreamApiException($"PokeAPI request timed out while looking up '{idOrName}'.", ex);
        }
    }

    public async Task<PokeApiListResponse> GetPokemonListAsync(int limit, int offset, CancellationToken ct)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"pokemon?limit={limit}&offset={offset}", ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PokeApiListResponse>(cancellationToken: ct);
            return result ?? new PokeApiListResponse();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "PokeAPI list request failed (limit={Limit}, offset={Offset}).", limit, offset);
            throw new UpstreamApiException("Could not reach PokeAPI while listing Pokemon.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "PokeAPI list request timed out (limit={Limit}, offset={Offset}).", limit, offset);
            throw new UpstreamApiException("PokeAPI request timed out while listing Pokemon.", ex);
        }
    }
}
