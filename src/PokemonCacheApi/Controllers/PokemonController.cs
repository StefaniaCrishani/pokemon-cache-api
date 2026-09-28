using Microsoft.AspNetCore.Mvc;
using PokemonCacheApi.Models.Dtos;
using PokemonCacheApi.Services;

namespace PokemonCacheApi.Controllers;

[ApiController]
[Route("api/pokemon")]
[Produces("application/json")]
public class PokemonController : ControllerBase
{
    private readonly IPokemonService _pokemonService;
    private readonly ILogger<PokemonController> _logger;

    public PokemonController(IPokemonService pokemonService, ILogger<PokemonController> logger)
    {
        _pokemonService = pokemonService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a page of Pokemon records. Served from the database cache; the very
    /// first call bootstraps the cache from PokeAPI (see README for details).
    /// </summary>
    /// <param name="limit">Page size, 1-100. Defaults to 20.</param>
    /// <param name="offset">Number of records to skip. Defaults to 0.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedPokemonResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedPokemonResponseDto>> GetList(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        if (limit is < 1 or > 100)
        {
            return BadRequest(new { message = "limit must be between 1 and 100." });
        }

        if (offset < 0)
        {
            return BadRequest(new { message = "offset must be zero or greater." });
        }

        _logger.LogInformation("GET /api/pokemon limit={Limit} offset={Offset}", limit, offset);

        var result = await _pokemonService.GetListAsync(limit, offset, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single Pokemon by numeric id (e.g. 25) or name (e.g. pikachu).
    /// Checks the database cache first; on a miss, fetches from PokeAPI, saves it, and returns it.
    /// </summary>
    [HttpGet("{idOrName}")]
    [ProducesResponseType(typeof(PokemonResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PokemonResponseDto>> GetOne(string idOrName, CancellationToken ct)
    {
        _logger.LogInformation("GET /api/pokemon/{IdOrName}", idOrName);

        var result = await _pokemonService.GetByIdOrNameAsync(idOrName, ct);

        if (result is null)
        {
            return NotFound(new { message = $"Pokemon '{idOrName}' was not found." });
        }

        return Ok(result);
    }
}
