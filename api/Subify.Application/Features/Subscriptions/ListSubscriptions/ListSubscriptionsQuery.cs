using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Services;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Subscriptions.ListSubscriptions;

/// <summary>
/// List current user's subscriptions (4.1.4) with financial summary (4.1.5 / 4.3.4).
/// Filters: includeArchived, category slug / ids, search; paginated.
/// Summary: active non-archived matching filters, converted to MainCurrency via FX snapshots.
/// </summary>
public sealed record ListSubscriptionsQuery(
    bool IncludeArchived = false,
    string? Category = null,
    Guid? CategoryId = null,
    Guid? UserCategoryId = null,
    string? Search = null,
    int Page = SubscriptionConstants.DefaultPage,
    int PageSize = SubscriptionConstants.DefaultPageSize)
    : IRequest<Result<ListSubscriptionsResponse>>;

public sealed record ListSubscriptionsResponse(
    IReadOnlyList<SubscriptionResponse> Data,
    PaginationInfo Pagination,
    SubscriptionListSummary Summary);

public sealed class ListSubscriptionsHandler
    : IRequestHandler<ListSubscriptionsQuery, Result<ListSubscriptionsResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IExchangeRateLookup _exchangeRates;

    public ListSubscriptionsHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IExchangeRateLookup exchangeRates)
    {
        _db = db;
        _currentUser = currentUser;
        _exchangeRates = exchangeRates;
    }

    public async Task<Result<ListSubscriptionsResponse>> Handle(
        ListSubscriptionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<ListSubscriptionsResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var page = request.Page < 1 ? SubscriptionConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize < 1
            ? SubscriptionConstants.DefaultPageSize
            : Math.Min(request.PageSize, SubscriptionConstants.MaxPageSize);

        var profile = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.MainCurrency, u.MonthlyBudget })
            .FirstOrDefaultAsync(cancellationToken);

        var mainCurrency = profile?.MainCurrency ?? SupportedCurrencies.Default;
        var monthlyBudget = profile?.MonthlyBudget;

        // Ignore soft-delete so archived rows are available when includeArchived=true.
        var query = _db.Subscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        query = ApplyFilters(query, request);

        var listQuery = request.IncludeArchived
            ? query
            : query.Where(s => !s.Archived && s.DeletedAt == null);

        var totalItems = await listQuery.CountAsync(cancellationToken);

        var items = await listQuery
            .IncludeDetails()
            .OrderBy(s => s.NextRenewalDate)
            .ThenBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var data = items.Select(SubscriptionResponse.FromEntity).ToList();

        // 4.1.5 / 4.3.3 — summary always from active non-archived matching filters (not page slice).
        var summaryRows = await query
            .Where(s => !s.Archived && s.DeletedAt == null)
            .Select(s => new
            {
                s.Price,
                s.SharedWithCount,
                s.BillingCycle,
                s.Currency
            })
            .ToListAsync(cancellationToken);

        var summaryLines = summaryRows.Select(r =>
            new SubscriptionAmountLine(r.Price, r.SharedWithCount, r.BillingCycle, r.Currency));

        var rates = await _exchangeRates.GetLatestRateMapAsync(cancellationToken);
        var totals = SubscriptionMath.SumConverted(summaryLines, mainCurrency, rates);

        return Result.Success(new ListSubscriptionsResponse(
            Data: data,
            Pagination: PaginationInfo.Create(page, pageSize, totalItems),
            Summary: SubscriptionListSummary.FromTotals(totals, monthlyBudget)));
    }

    private IQueryable<Domain.Entities.Subscription> ApplyFilters(
        IQueryable<Domain.Entities.Subscription> query,
        ListSubscriptionsQuery request)
    {
        if (request.CategoryId is { } categoryId)
        {
            query = query.Where(s => s.CategoryId == categoryId);
        }

        if (request.UserCategoryId is { } userCategoryId)
        {
            query = query.Where(s => s.UserCategoryId == userCategoryId);
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var slug = request.Category.Trim().ToLowerInvariant();
            var categoryIds = _db.Categories
                .AsNoTracking()
                .Where(c => c.Slug == slug)
                .Select(c => c.Id);

            query = query.Where(s => s.CategoryId != null && categoryIds.Contains(s.CategoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(s =>
                s.Name.ToLower().Contains(term)
                || (s.Notes != null && s.Notes.ToLower().Contains(term)));
        }

        return query;
    }
}
