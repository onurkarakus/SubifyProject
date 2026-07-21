namespace Subify.Domain.Shared;

public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, string.Empty, ErrorType.None);
    public static readonly Error NullValue = new("Error.NullValue", "Null Valuer Exception", "Value Can Not Be Null", ErrorType.Failure);

    public string Code { get; }
    public string Title { get; }
    public string Description { get; }
    public ErrorType Type { get; }

    public Error(string code, string title, string description, ErrorType type)
    {
        Code = code;
        Title = title;
        Description = description;
        Type = type;
    }

    public static Error Failure(string code, string title, string description) => new(code, title, description, ErrorType.Failure);
    public static Error NotFound(string code, string title, string description) => new(code, title, description, ErrorType.NotFound);
    public static Error Validation(string code, string title, string description) => new(code, title, description, ErrorType.Validation);
    public static Error Unauthorized(string code, string title, string description) => new(code, title, description, ErrorType.Unauthorized);
    public static Error Conflict(string code, string title, string description) => new(code, title, description, ErrorType.Conflict);
    public static Error Locked(string code, string title, string description) => new(code, title, description, ErrorType.Locked);
    public static Error Forbidden(string code, string title, string description) => new(code, title, description, ErrorType.Forbidden);
    public static Error TooManyRequest(string code, string title, string description) => new(code, title, description, ErrorType.TooManyRequest);
    public static Error ServiceUnavailable(string code, string title, string description) => new(code, title, description, ErrorType.ServiceUnavailable);
    public static Error InternalServerError(string code, string title, string description) => new(code, title, description, ErrorType.InternalServerError);
}