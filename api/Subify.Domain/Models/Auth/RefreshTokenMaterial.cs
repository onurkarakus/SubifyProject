namespace Subify.Domain.Models.Auth;

/// <summary>
/// Fresh refresh token material (task 3.1.2).
/// <see cref="PlainText"/> is returned to the client only; <see cref="TokenHash"/> is what we persist.
/// </summary>
public sealed record RefreshTokenMaterial(
    string PlainText,
    string TokenHash,
    DateTimeOffset ExpiresAt);
