namespace Subify.Application.Features.Auth.Register;

/// <summary>
/// Successful registration payload (task 3.2.1).
/// No tokens here — client should call login (or setup flow for first SuperAdmin later).
/// </summary>
public sealed record RegisterResponse(
    string UserId,
    string Email,
    string FullName,
    string Message);
