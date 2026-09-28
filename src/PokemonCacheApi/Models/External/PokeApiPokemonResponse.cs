using System.Text.Json.Serialization;

namespace PokemonCacheApi.Models.External;

/// <summary>
/// Minimal subset of the fields returned by https://pokeapi.co/api/v2/pokemon/{idOrName}.
/// Only what we actually persist/return is mapped; the real payload has many more fields.
/// </summary>
public class PokeApiPokemonResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("weight")]
    public int Weight { get; set; }

    [JsonPropertyName("types")]
    public List<PokeApiTypeSlot> Types { get; set; } = new();

    [JsonPropertyName("sprites")]
    public PokeApiSprites? Sprites { get; set; }
}

public class PokeApiTypeSlot
{
    [JsonPropertyName("slot")]
    public int Slot { get; set; }

    [JsonPropertyName("type")]
    public PokeApiTypeInfo Type { get; set; } = new();
}

public class PokeApiTypeInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class PokeApiSprites
{
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }
}
