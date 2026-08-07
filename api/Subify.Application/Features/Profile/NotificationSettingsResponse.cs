namespace Subify.Application.Features.Profile;

/// <summary>Per-user notification preferences (5.3.5). EmailEnabled gates renewal reminder mail when SMTP is on.</summary>
public sealed record NotificationSettingsResponse(
    bool EmailEnabled,
    bool PushEnabled,
    int DaysBeforeRenewal);
