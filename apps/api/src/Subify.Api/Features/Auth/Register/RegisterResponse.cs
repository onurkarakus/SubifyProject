namespace Subify.Api.Features.Auth.Register;

public record RegisterResponse(string Email, string Message, string UserId, DateTime Expiration);
