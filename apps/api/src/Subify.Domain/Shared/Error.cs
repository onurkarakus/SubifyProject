namespace Subify.Domain.Shared;

public enum ErrorType
{
    None = 0,         // Hatasız durum
    Failure = 1,      // Genel hata (400 Bad Request)
    Validation = 2,   // Validasyon hatası (400 Bad Request)
    NotFound = 3,     // Bulunamadı (404 Not Found)
    Conflict = 4,     // Çakışma (409 Conflict)
    Unauthorized = 5, // Yetkisiz (401 Unauthorized)
    Forbidden = 6,   // Yasaklı (403 Forbidden)
    Locked = 7,        // Kilitli (423 Locked)
    TooManyRequest = 8, // Çok fazla istek (429 Too Many Requests)
    ServiceUnavailable = 9, // Servis Kullanılamıyor (503 Service Unavailable)
    InternalServerError = 10 // Dahili Sunucu Hatası (500 Internal Server Error)
}

public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, string.Empty, ErrorType.None);
    public static readonly Error NullValue = new("Error.NullValue", "Null Value Exception", "Value can not be null.", ErrorType.Failure);

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
