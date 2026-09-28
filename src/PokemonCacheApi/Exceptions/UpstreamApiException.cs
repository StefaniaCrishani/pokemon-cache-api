namespace PokemonCacheApi.Exceptions;

/// <summary>
/// Thrown when the third-party API (PokeAPI) is unreachable or returns an
/// unexpected error. Kept distinct from "not found" so the middleware can
/// map it to 502 Bad Gateway instead of 500.
/// </summary>
public class UpstreamApiException : Exception
{
    public UpstreamApiException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
