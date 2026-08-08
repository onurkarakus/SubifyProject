using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Setup.UpdateSetupAi;

/// <summary>Setup step: BYOK AI key (3S.6.1). Stored only; used later by AI features.</summary>
public sealed record UpdateSetupAiCommand(
    string? AiProvider,
    string? AiApiKey,
    string? AiModel,
    string? AiBaseUrl = null) : IRequest<Result>;

public sealed class UpdateSetupAiValidator : AbstractValidator<UpdateSetupAiCommand>
{
    public UpdateSetupAiValidator()
    {
        RuleFor(x => x.AiProvider).MaximumLength(100).When(x => x.AiProvider is not null);
        RuleFor(x => x.AiModel).MaximumLength(200).When(x => x.AiModel is not null);
        RuleFor(x => x.AiBaseUrl).MaximumLength(500).When(x => x.AiBaseUrl is not null);
    }
}

public sealed class UpdateSetupAiHandler : IRequestHandler<UpdateSetupAiCommand, Result>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateSetupAiHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateSetupAiCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        var settings = await _db.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            return Result.Failure(DomainErrors.Setup.SettingsNotInitialized);
        }

        if (settings.IsSetupComplete)
        {
            return Result.Failure(DomainErrors.Setup.AlreadyComplete);
        }

        settings.UpdateAi(
            aiProvider: request.AiProvider,
            aiApiKey: request.AiApiKey,
            aiModel: request.AiModel,
            aiBaseUrl: request.AiBaseUrl);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
