using Subify.Domain.Shared;

namespace Subify.Api.Common.Extensions;

/// <summary>
/// Single source of truth: <see cref="ErrorType"/> → HTTP status code for RFC 7807 ProblemDetails.
/// </summary>
/// <remarks>
/// <para>Map (verified task 1.2.10):</para>
/// <list type="table">
/// <listheader><term>ErrorType</term><description>HTTP</description></listheader>
/// <item><term>Validation</term><description>400 Bad Request</description></item>
/// <item><term>Failure</term><description>400 Bad Request (business rule / client error)</description></item>
/// <item><term>Unauthorized</term><description>401 Unauthorized</description></item>
/// <item><term>Forbidden</term><description>403 Forbidden</description></item>
/// <item><term>NotFound</term><description>404 Not Found</description></item>
/// <item><term>Conflict</term><description>409 Conflict</description></item>
/// <item><term>Locked</term><description>423 Locked</description></item>
/// <item><term>TooManyRequest</term><description>429 Too Many Requests</description></item>
/// <item><term>InternalServerError</term><description>500 Internal Server Error</description></item>
/// <item><term>ServiceUnavailable</term><description>503 Service Unavailable</description></item>
/// <item><term>GatewayTimeout</term><description>504 Gateway Timeout</description></item>
/// <item><term>None / unknown</term><description>500 (should not be returned as failure)</description></item>
/// </list>
/// </remarks>
public static class ProblemDetailsStatusMapper
{
    public static int ToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Failure => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Locked => StatusCodes.Status423Locked,
        ErrorType.TooManyRequest => StatusCodes.Status429TooManyRequests,
        ErrorType.InternalServerError => StatusCodes.Status500InternalServerError,
        ErrorType.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
        ErrorType.GatewayTimeout => StatusCodes.Status504GatewayTimeout,
        ErrorType.None => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };

    public static int ToStatusCode(Error error) => ToStatusCode(error.Type);

    /// <summary>
    /// All known <see cref="ErrorType"/> values except <see cref="ErrorType.None"/>.
    /// Used to assert the switch stays exhaustive in tests / diagnostics.
    /// </summary>
    public static IReadOnlyDictionary<ErrorType, int> AllMappings { get; } =
        Enum.GetValues<ErrorType>()
            .Where(t => t != ErrorType.None)
            .ToDictionary(t => t, ToStatusCode);

    public static bool IsClientError(ErrorType errorType)
    {
        var status = ToStatusCode(errorType);
        return status is >= 400 and < 500;
    }

    public static bool IsServerError(ErrorType errorType)
    {
        var status = ToStatusCode(errorType);
        return status is >= 500 and < 600;
    }
}
