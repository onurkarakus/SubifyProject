using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subify.Domain.Enums;

public enum SubscriptionStatus
{
    Active,
    Paused,
    Cancelled,
    Overdue,
    Archived
}
