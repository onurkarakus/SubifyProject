namespace Subify.Domain.Shared;

public enum ErrorType
{
    None = 0,
    Failure = 1,
    Validation = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5,
    Forbidden = 6,
    Locked = 7,
    TooManyRequest = 8,
    ServiceUnavailable = 9,
    InternalServerError = 10
}