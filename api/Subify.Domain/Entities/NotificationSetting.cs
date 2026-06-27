using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class NotificationSetting : BaseEntity
{
    public Guid UserId { get; private set; }
    public bool EmailEnabled { get; private set; } = true;
    public bool PushEnabled { get; private set; } = false;
    public int DaysBeforeRenewal { get; private set; } = 3;

    public ApplicationUser User { get; private set; } = null!;

    protected NotificationSetting() { }

    public NotificationSetting(Guid userId)
    {
        UserId = userId;
    }

    public void UpdateSettings(bool emailEnabled, bool pushEnabled, int daysBeforeRenewal)
    {
        EmailEnabled = emailEnabled;
        PushEnabled = pushEnabled;
        DaysBeforeRenewal = daysBeforeRenewal;
    }
}