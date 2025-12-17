using Subify.Domain.Enums;
using Subify.Domain.Models.Entities.Common;
using Subify.Domain.Models.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subify.Domain.Models.Entities.ApplicationPayments;

/// <summary>
/// Tracks RevenueCat/Stripe checkout sessions.
/// </summary>
public sealed class BillingSession : BaseEntity
{
    public Guid? UserId { get; set; }

    /// <summary>
    /// Payment provider: 'revenuecat', 'stripe'
    /// </summary>
    public string Provider { get; set; } = "revenuecat";

    /// <summary>
    /// External checkout session ID.
    /// </summary>
    public string SessionId { get; set; } = null!;

    /// <summary>
    /// Selected plan: 'monthly', 'yearly', 'lifetime'
    /// </summary>
    public string Plan { get; set; } = null!;

    /// <summary>
    /// Session status. 
    /// </summary>
    public BillingSessionStatus Status { get; set; } = BillingSessionStatus.Pending;

    /// <summary>
    /// Checkout URL for redirect.
    /// </summary>
    public string? CheckoutUrl { get; set; }

    /// <summary>
    /// When the session expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// When payment was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation
    public ApplicationUser? User { get; set; } = null!;
}
