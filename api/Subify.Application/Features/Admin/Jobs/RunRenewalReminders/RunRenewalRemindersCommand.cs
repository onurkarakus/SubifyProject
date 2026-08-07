using MediatR;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Jobs.RunRenewalReminders;

/// <summary>
/// 8.1 ops — SuperAdmin can force one renewal-reminder scan (same logic as background job).
/// </summary>
public sealed record RunRenewalRemindersCommand : IRequest<Result<RunRenewalRemindersResponse>>;

public sealed record RunRenewalRemindersResponse(int ProcessedCount);

public sealed class RunRenewalRemindersHandler
    : IRequestHandler<RunRenewalRemindersCommand, Result<RunRenewalRemindersResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRenewalReminderService _reminders;

    public RunRenewalRemindersHandler(
        ICurrentUserService currentUser,
        IRenewalReminderService reminders)
    {
        _currentUser = currentUser;
        _reminders = reminders;
    }

    public async Task<Result<RunRenewalRemindersResponse>> Handle(
        RunRenewalRemindersCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<RunRenewalRemindersResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<RunRenewalRemindersResponse>(DomainErrors.SystemSettingsErrors.AccessDenied);
        }

        var count = await _reminders.ProcessDueRemindersAsync(cancellationToken);
        return Result.Success(new RunRenewalRemindersResponse(count));
    }
}
