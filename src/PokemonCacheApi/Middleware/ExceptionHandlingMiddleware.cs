using System.Net;
using Microsoft.Data.SqlClient;
using PokemonCacheApi.Exceptions;

namespace PokemonCacheApi.Middleware;

/// <summary>
/// Catches anything that bubbles up from the controllers so callers always get a
/// consistent JSON error shape instead of a raw exception / stack trace, and so
/// upstream API failures are distinguishable from database or unexpected errors.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UpstreamApiException ex)
        {
            _logger.LogError(ex, "Upstream API call failed.");
            await WriteProblemAsync(context, HttpStatusCode.BadGateway, "Upstream API error", ex.Message);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error.");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Database error",
                "A database error occurred while processing the request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Unexpected error",
                "An unexpected error occurred while processing the request.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problem = new
        {
            title,
            status = (int)statusCode,
            detail,
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
