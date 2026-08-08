namespace Subify.Application.Features.Admin.Invites;

/// <summary>
/// Create-invite response includes plain token once (7.2.1).
/// <see cref="EmailSent"/> is true when SMTP send succeeded (15.2.2); link is always present.
/// </summary>
public sealed record CreateInviteResponse(
    Guid Id,
    string Email,
    string Token,
    string InviteUrl,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    bool EmailSent = false);

/// <summary>Pending invite list item — never includes plain token (7.2.2).</summary>
public sealed record InviteListItemResponse(
    Guid Id,
    string Email,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    Guid CreatedByUserId,
    bool IsPending);

public sealed record ListInvitesResponse(IReadOnlyList<InviteListItemResponse> Data);
