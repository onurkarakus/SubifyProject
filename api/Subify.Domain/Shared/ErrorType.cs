namespace Subify.Domain.Shared;

/// <summary>
/// Domain error categories. HTTP mapping lives in API
/// <c>ProblemDetailsStatusMapper</c> (task 1.2.10).
/// </summary>
public enum ErrorType
{
    None = 0,
    /// <summary>Generic client/business failure (maps to 400).</summary>
    Failure = 1,
    /// <summary>Input validation (maps to 400 + errors bag).</summary>
    Validation = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5,
    Forbidden = 6,
    Locked = 7,
    TooManyRequest = 8,
    ServiceUnavailable = 9,
    InternalServerError = 10,
    /// <summary>Upstream/gateway timeout (maps to 504).</summary>
    GatewayTimeout = 11
}
