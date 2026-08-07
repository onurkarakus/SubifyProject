using System.Diagnostics;
using MediatR;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Admin.Settings.TestAi;

/// <summary>
/// 7.3.4 — SuperAdmin minimal LLM ping with stored BYOK settings.
/// Does not count toward user AI daily analyze quotas.
/// </summary>
public sealed record TestAiCommand : IRequest<Result<TestAiResponse>>;

public sealed record TestAiResponse(
    bool Ok,
    string Model,
    string? Provider,
    int LatencyMs,
    string ReplyPreview);

public sealed class TestAiHandler : IRequestHandler<TestAiCommand, Result<TestAiResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAiSettingsResolver _settingsResolver;
    private readonly IAiClient _aiClient;

    public TestAiHandler(
        ICurrentUserService currentUser,
        IAiSettingsResolver settingsResolver,
        IAiClient aiClient)
    {
        _currentUser = currentUser;
        _settingsResolver = settingsResolver;
        _aiClient = aiClient;
    }

    public async Task<Result<TestAiResponse>> Handle(
        TestAiCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<TestAiResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<TestAiResponse>(DomainErrors.SystemSettingsErrors.AccessDenied);
        }

        var settings = await _settingsResolver.ResolveAsync(cancellationToken);
        if (settings.IsFailure)
        {
            return Result.Failure<TestAiResponse>(settings.Error);
        }

        var runtime = settings.Value;
        var sw = Stopwatch.StartNew();

        var completion = await _aiClient.CompleteAsync(
            new AiChatCompletionRequest(
                ApiKey: runtime.ApiKey,
                Model: runtime.Model,
                BaseUrl: runtime.BaseUrl,
                Messages:
                [
                    new AiChatMessage(
                        "system",
                        "You are a connectivity probe for Subify OS. Reply with the single word: pong"),
                    new AiChatMessage("user", "ping")
                ],
                Temperature: 0,
                RequireJsonObjectResponse: false),
            cancellationToken);

        sw.Stop();

        if (completion.IsFailure)
        {
            return Result.Failure<TestAiResponse>(completion.Error);
        }

        var content = completion.Value.Content.Trim();
        var preview = content.Length <= 200 ? content : content[..200] + "…";

        return Result.Success(new TestAiResponse(
            Ok: true,
            Model: completion.Value.Model ?? runtime.Model,
            Provider: runtime.Provider,
            LatencyMs: (int)Math.Min(sw.ElapsedMilliseconds, int.MaxValue),
            ReplyPreview: preview));
    }
}
