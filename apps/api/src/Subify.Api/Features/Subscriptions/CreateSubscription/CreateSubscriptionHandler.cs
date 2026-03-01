using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Enums;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Providers;
using Subify.Domain.Models.Entities.Subscriptions;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Security.Claims;

namespace Subify.Api.Features.Subscriptions.CreateSubscription;

public class CreateSubscriptionHandler : IRequestHandler<CreateSubscriptionCommand, Result<Guid>>
{
    private readonly SubifyDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateSubscriptionHandler(SubifyDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<Guid>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result.Failure<Guid>(DomainErrors.UserErrors.UnAuthorized);
        }

        Provider? provider = null;

        if (request.ProviderId.HasValue)
        {
            provider = await _context.Providers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ProviderId.Value, cancellationToken);

            if (provider == null)
            {
                return Result.Failure<Guid>(DomainErrors.ProviderErrors.NotFound);
            }
        }

        var nextPaymentDate = CalculateNextPayment(request.StartDate, request.BillingCycle);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Description = request.Decsription ?? string.Empty,
            ProviderId = request.ProviderId,
            CategoryId = request.CategoryId,
            UserCategoryId = request.UserCategoryId,
            Amount = request.Amount,
            Currency = request.Currency.ToUpper(),
            BillingCycle = request.BillingCycle,
            StartDate = request.StartDate,
            NextPaymentDate = nextPaymentDate,
            RemindMe = request.RemindMe,
            ReminderDaysBefore = request.ReminderDaysBefore,
            Status = SubscriptionStatus.Active,
            Archived = false,
            CreatedAt = DateTime.UtcNow,
            Icon = (provider != null && string.IsNullOrEmpty(request.Icon))
                   ? provider.LogoUrl
                   : request.Icon,
            Color = request.Color
        };

        if (string.IsNullOrEmpty(subscription.Name) && (provider != null))
        {
            subscription.Name = provider.Name;
        }

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(subscription.Id);
    }

    private static DateOnly CalculateNextPayment(DateTime startDate, BillingCycle billingCycle)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nextDate = DateOnly.FromDateTime(startDate);

        if (nextDate > today)
        {
            return nextDate;
        }

        while (nextDate <= today)
        {
            nextDate = billingCycle switch
            {
                BillingCycle.Weekly => nextDate.AddDays(7),
                BillingCycle.Monthly => nextDate.AddMonths(1),
                BillingCycle.Yearly => nextDate.AddYears(1),
                _ => nextDate.AddMonths(1)
            };
        }

        return nextDate;
    }
}
