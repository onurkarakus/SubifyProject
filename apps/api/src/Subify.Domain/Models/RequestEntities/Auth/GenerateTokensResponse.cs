namespace Subify.Domain.Models.RequestEntities.Auth;

public record GenerateTokensResponse()
{
    public GenerateTokensResponse(string accessToken, string refreshToken, DateTime expiration) : this()
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        Expiration = expiration;
    }

    public string AccessToken { get; init; }

    public string RefreshToken { get; init; }

    public DateTime Expiration { get; init; }
}
