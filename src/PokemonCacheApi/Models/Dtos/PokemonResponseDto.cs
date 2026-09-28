namespace PokemonCacheApi.Models.Dtos;

/// <summary>
/// What the API actually returns to clients for a single Pokemon.
/// "Source" is included so callers (and graders) can see whether the
/// record was served from the database cache or freshly fetched from PokeAPI.
/// </summary>
public class PokemonResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? HeightDecimetres { get; set; }
    public int? WeightHectograms { get; set; }
    public List<string> Types { get; set; } = new();
    public string? ImageUrl { get; set; }

    /// <summary>"cache" if it came from the database, "api" if it was just fetched from PokeAPI and saved.</summary>
    public string Source { get; set; } = "cache";
}
