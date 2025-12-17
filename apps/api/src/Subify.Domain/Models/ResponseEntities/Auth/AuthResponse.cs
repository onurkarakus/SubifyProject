namespace Subify.Domain.Models.ResponseEntities.Auth;

public record AuthResponse() 
{
    public AuthResponse(string email, string accessToken, string refreshToken, DateTime expiration)
        : this()
    {
        Email = email;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        Expiration = expiration;       
    }

    public string Email { get; init; }

    public string AccessToken { get; init; }

    public string RefreshToken { get; init; }

    public DateTime Expiration { get; init; }
};
