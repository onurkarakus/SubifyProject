using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Subify.Api.Helpers;
using Subify.Domain.Enums;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Providers;
using Subify.Domain.Models.Entities.Subscriptions;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace Subify.Api.Features.Subscriptions.GetSubscriptions;

public class GetSubscriptionsHandler : IRequestHandler<GetSubscriptionsQuery, Result<List<GetSubscriptionsResponse>>>
{
    private readonly SubifyDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetSubscriptionsHandler(SubifyDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<List<GetSubscriptionsResponse>>> Handle(GetSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result.Failure<List<GetSubscriptionsResponse>>(DomainErrors.UserErrors.UnAuthorized);
        }

        ResourceHelper resourceHelper = new(_context);


        var subscriptions = await _context.Subscriptions
            .Where(s => s.UserId == userId && !s.Archived)
            .Select(s => new GetSubscriptionsResponse(
                s.Id,
                s.UserId,
                s.ProviderId,
                s.Provider != null ? s.Provider.Name : null,
                s.CategoryId,
                s.Category != null ? s.Category.Slug : null,
                s.UserCategoryId,
                s.UserCategory != null ? s.UserCategory.Name : null,
                s.Name,
                s.Description,
                s.Icon,
                s.Color,
                s.Amount,
                s.Currency,
                s.BillingCycle,
                s.StartDate,
                s.NextPaymentDate,
                s.RemindMe,
                s.ReminderDaysBefore,
                s.Status,
                s.CreatedAt.Date,
                s.Archived
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(subscriptions);
    }
}
