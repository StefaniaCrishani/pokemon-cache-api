namespace PokemonCacheApi.Models.Dtos;

public class PagedPokemonResponseDto
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public int TotalCached { get; set; }
    public string Source { get; set; } = "cache";
    public List<PokemonResponseDto> Items { get; set; } = new();
}
