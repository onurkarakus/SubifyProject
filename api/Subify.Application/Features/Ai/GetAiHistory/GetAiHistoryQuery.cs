using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Ai.GetAiHistory;

/// <summary>Paginated AI analyze history for current user (9.2.2).</summary>
public sealed record GetAiHistoryQuery(
    int Page = 1,
    int PageSize = 10) : IRequest<Result<ListAiHistoryResponse>>;

public sealed class GetAiHistoryValidator : AbstractValidator<GetAiHistoryQuery>
{
    public GetAiHistoryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, SubscriptionConstants.MaxPageSize);
    }
}

public sealed class GetAiHistoryHandler
    : IRequestHandler<GetAiHistoryQuery, Result<ListAiHistoryResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAiHistoryHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ListAiHistoryResponse>> Handle(
        GetAiHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<ListAiHistoryResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, SubscriptionConstants.MaxPageSize);

        // Materialize for SQLite DateTimeOffset ordering
        var all = await _db.AISuggestionLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .ToListAsync(cancellationToken);

        var ordered = all
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .ToList();

        var total = ordered.Count;
        var slice = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l =>
            {
                var summary = "AI analysis";
                decimal monthly = 0;
                decimal yearly = 0;
                try
                {
                    var parsed = JsonSerializer.Deserialize<HistoryPayload>(l.ResponsePayload, JsonOptions);
                    if (parsed is not null)
                    {
                        if (!string.IsNullOrWhiteSpace(parsed.Summary))
                        {
                            summary = parsed.Summary;
                        }

                        monthly = parsed.EstimatedMonthlySaving;
                        yearly = parsed.EstimatedYearlySaving;
                    }
                }
                catch (JsonException)
                {
                    // keep defaults
                }

                return new AiHistoryItemResponse(
                    Id: l.Id,
                    Summary: summary,
                    EstimatedMonthlySaving: monthly,
                    EstimatedYearlySaving: yearly,
                    CreatedAt: l.CreatedAt);
            })
            .ToList();

        return Result.Success(new ListAiHistoryResponse(
            Data: slice,
            Pagination: PaginationInfo.Create(page, pageSize, total)));
    }

    private sealed class HistoryPayload
    {
        public string? Summary { get; set; }
        public decimal EstimatedMonthlySaving { get; set; }
        public decimal EstimatedYearlySaving { get; set; }
    }
}
