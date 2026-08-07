using Subify.Domain.Common;

namespace Subify.Domain.Entities;

/// <summary>
/// Single-use invite for multi-user onboarding (link always in API/UI; optional email when SMTP configured).
/// Plain token is never stored — only <see cref="TokenHash"/>.
/// </summary>
public class UserInvite : BaseEntity
{
    public const int EmailMaxLength = 320;
    public const int TokenHashMaxLength = 128;
    public const int DefaultExpiryDays = 7;

    public string Email { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Admin / SuperAdmin who created the invite.</summary>
    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }
    public Guid? AcceptedUserId { get; private set; }

    public ApplicationUser CreatedByUser { get; private set; } = null!;
    public ApplicationUser? AcceptedUser { get; private set; }

    public bool IsUsed => UsedAt.HasValue;

    public bool IsExpired(DateTimeOffset? utcNow = null) =>
        (utcNow ?? DateTimeOffset.UtcNow) >= ExpiresAt;

    /// <summary>Pending = not used and not expired.</summary>
    public bool IsPending(DateTimeOffset? utcNow = null) =>
        !IsUsed && !IsExpired(utcNow);

    protected UserInvite()
    {
    }

    public static UserInvite Create(
        string email,
        string tokenHash,
        Guid createdByUserId,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? utcNow = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentOutOfRangeException.ThrowIfEqual(createdByUserId, Guid.Empty);

        var now = utcNow ?? DateTimeOffset.UtcNow;
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return new UserInvite
        {
            Id = GuidGenerator.NewId(),
            Email = normalizedEmail,
            TokenHash = tokenHash.Trim(),
            CreatedByUserId = createdByUserId,
            ExpiresAt = expiresAt ?? now.AddDays(DefaultExpiryDays),
            CreatedAt = now
        };
    }

    /// <summary>
    /// Marks invite as consumed when the invitee registers via accept-invite.
    /// </summary>
    public bool TryMarkUsed(Guid acceptedUserId, DateTimeOffset? utcNow = null)
    {
        var now = utcNow ?? DateTimeOffset.UtcNow;

        if (IsUsed || IsExpired(now) || acceptedUserId == Guid.Empty)
        {
            return false;
        }

        UsedAt = now;
        AcceptedUserId = acceptedUserId;
        UpdatedAt = now;
        return true;
    }

    public void MarkUsed(Guid acceptedUserId, DateTimeOffset? utcNow = null)
    {
        if (!TryMarkUsed(acceptedUserId, utcNow))
        {
            throw new InvalidOperationException(
                "Invite cannot be marked as used (already used, expired, or invalid user).");
        }
    }

    /// <summary>
    /// Expires a still-pending invite (e.g. superseded by a newer invite for the same email).
    /// </summary>
    public void ExpireNow(DateTimeOffset? utcNow = null)
    {
        if (IsUsed)
        {
            return;
        }

        var now = utcNow ?? DateTimeOffset.UtcNow;
        if (ExpiresAt > now)
        {
            ExpiresAt = now;
            UpdatedAt = now;
        }
    }
}
