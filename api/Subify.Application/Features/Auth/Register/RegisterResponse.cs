namespace Subify.Application.Features.Auth.Register;

public record RegisterResponse(string Email, string Message, string UserId, DateTime Expiration);