using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subify.Domain.Enums;

public enum BillingSessionStatus
{
    Pending,
    Paid,
    Failed,
    Expired,
    Cancelled
}
