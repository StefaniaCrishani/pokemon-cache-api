namespace PokemonCacheApi.Models;

/// <summary>
/// Represents a row in the dbo.Pokemon table. This is the "local cache" copy
/// of a Pokemon that either came from a previous API call or was just fetched.
/// </summary>
public class PokemonRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? HeightDecimetres { get; set; }
    public int? WeightHectograms { get; set; }

    public string? Types { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
