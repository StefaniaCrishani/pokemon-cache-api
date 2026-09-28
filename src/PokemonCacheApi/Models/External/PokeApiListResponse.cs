using System.Text.Json.Serialization;

namespace PokemonCacheApi.Models.External;

/// <summary>Shape of https://pokeapi.co/api/v2/pokemon?limit=&offset= </summary>
public class PokeApiListResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("results")]
    public List<PokeApiListItem> Results { get; set; } = new();
}

public class PokeApiListItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
