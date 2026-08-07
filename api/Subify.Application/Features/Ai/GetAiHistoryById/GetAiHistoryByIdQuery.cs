using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

// AiHistoryDetailResponse, AiTipDto, AiTipTypes live in parent namespace Features.Ai
namespace Subify.Application.Features.Ai.GetAiHistoryById;

/// <summary>Full AI analysis entry for current user (history detail).</summary>
public sealed record GetAiHistoryByIdQuery(Guid Id) : IRequest<Result<AiHistoryDetailResponse>>;

public sealed class GetAiHistoryByIdHandler
    : IRequestHandler<GetAiHistoryByIdQuery, Result<AiHistoryDetailResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAiHistoryByIdHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<AiHistoryDetailResponse>> Handle(
        GetAiHistoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<AiHistoryDetailResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var log = await _db.AISuggestionLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.Id == request.Id && l.UserId == userId,
                cancellationToken);

        if (log is null)
        {
            return Result.Failure<AiHistoryDetailResponse>(DomainErrors.AiErrors.HistoryNotFound);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<StoredPayload>(log.ResponsePayload, JsonOptions);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Summary))
            {
                return Result.Failure<AiHistoryDetailResponse>(DomainErrors.AiErrors.ProcessingError);
            }

            var tips = (payload.Tips ?? [])
                .Where(t => !string.IsNullOrWhiteSpace(t.Message))
                .Select(t => new AiTipDto(
                    Type: AiTipTypes.Normalize(t.Type),
                    Message: t.Message!.Trim(),
                    PotentialSaving: t.PotentialSaving is > 0 ? t.PotentialSaving : null,
                    SubscriptionId: t.SubscriptionId,
                    SubscriptionName: string.IsNullOrWhiteSpace(t.SubscriptionName)
                        ? null
                        : t.SubscriptionName.Trim()))
                .ToList();

            var monthly = Math.Max(0, payload.EstimatedMonthlySaving);
            var yearly = payload.EstimatedYearlySaving > 0
                ? payload.EstimatedYearlySaving
                : monthly * 12m;

            return Result.Success(new AiHistoryDetailResponse(
                Id: log.Id,
                Summary: payload.Summary.Trim(),
                Tips: tips,
                EstimatedMonthlySaving: monthly,
                EstimatedYearlySaving: yearly,
                AnalyzedAt: payload.AnalyzedAt == default ? log.CreatedAt : payload.AnalyzedAt,
                CreatedAt: log.CreatedAt));
        }
        catch (JsonException)
        {
            return Result.Failure<AiHistoryDetailResponse>(DomainErrors.AiErrors.ProcessingError);
        }
    }

    private sealed class StoredPayload
    {
        public string? Summary { get; set; }
        public List<StoredTip>? Tips { get; set; }
        public decimal EstimatedMonthlySaving { get; set; }
        public decimal EstimatedYearlySaving { get; set; }
        public DateTimeOffset AnalyzedAt { get; set; }
    }

    private sealed class StoredTip
    {
        public string? Type { get; set; }
        public string? Message { get; set; }
        public decimal? PotentialSaving { get; set; }
        public Guid? SubscriptionId { get; set; }
        public string? SubscriptionName { get; set; }
    }
}
