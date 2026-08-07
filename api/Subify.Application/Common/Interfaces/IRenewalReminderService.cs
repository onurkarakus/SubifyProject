namespace Subify.Application.Common.Interfaces;

/// <summary>
/// 15.3.1 — scan users with email notifications and send renewal reminders (deduped).
/// </summary>
public interface IRenewalReminderService
{
    /// <returns>Number of successful send attempts (including dedupe skips counted as success).</returns>
    Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}
