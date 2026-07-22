using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Logout;

/// <summary>Revoke a refresh token (task 3.2.4). Optional: revoke all sessions for current user.</summary>
public sealed record LogoutCommand(
    string? RefreshToken,
    bool AllSessions = false) : IRequest<Result>;
