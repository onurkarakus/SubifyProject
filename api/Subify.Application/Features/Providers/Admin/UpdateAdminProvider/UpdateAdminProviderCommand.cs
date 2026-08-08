using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Providers.Admin.UpdateAdminProvider;

/// <summary>SuperAdmin: update catalog provider (5.2.3).</summary>
public sealed record UpdateAdminProviderCommand(
    Guid Id,
    string Name,
    string Slug,
    string Currency,
    string BillingCycle,
    string Region,
    decimal? Price = null,
    decimal? PriceBefore = null,
    string? SourceUrl = null,
    string? LogoUrl = null,
    bool IsActive = true) : IRequest<Result<ProviderResponse>>;

public sealed class UpdateAdminProviderValidator : AbstractValidator<UpdateAdminProviderCommand>
{
    public UpdateAdminProviderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase kebab-case.");
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10)
            .Must(SupportedCurrencies.IsSupported);
        RuleFor(x => x.BillingCycle).NotEmpty()
            .Must(CreateSubscriptionValidator.BeSupportedBillingCycle);
        RuleFor(x => x.Region).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price is not null);
        RuleFor(x => x.PriceBefore).GreaterThan(0).When(x => x.PriceBefore is not null);
        RuleFor(x => x.SourceUrl).MaximumLength(500).When(x => x.SourceUrl is not null);
        RuleFor(x => x.LogoUrl).MaximumLength(500).When(x => x.LogoUrl is not null);
    }
}

public sealed class UpdateAdminProviderHandler
    : IRequestHandler<UpdateAdminProviderCommand, Result<ProviderResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateAdminProviderHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ProviderResponse>> Handle(
        UpdateAdminProviderCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<ProviderResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!CreateSubscriptionValidator.TryParseBillingCycle(request.BillingCycle, out var cycle))
        {
            return Result.Failure<ProviderResponse>(DomainErrors.Subscription.InvalidBillingCycle);
        }

        var entity = await _db.Providers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure<ProviderResponse>(DomainErrors.ProviderErrors.NotFound);
        }

        var slug = request.Slug.Trim().ToLowerInvariant();
        var name = request.Name.Trim();

        if (await _db.Providers.IgnoreQueryFilters().AnyAsync(
                p => p.Id != entity.Id && p.Slug == slug, cancellationToken))
        {
            return Result.Failure<ProviderResponse>(DomainErrors.ProviderErrors.DuplicateSlug);
        }

        if (await _db.Providers.IgnoreQueryFilters().AnyAsync(
                p => p.Id != entity.Id && p.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            return Result.Failure<ProviderResponse>(DomainErrors.ProviderErrors.DuplicateName);
        }

        entity.Update(
            name: name,
            slug: slug,
            logoUrl: string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim(),
            currency: SupportedCurrencies.Normalize(request.Currency),
            price: request.Price,
            priceBefore: request.PriceBefore,
            billingCycle: cycle,
            region: request.Region.Trim().ToUpperInvariant(),
            sourceUrl: string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim(),
            lastVerifiedAt: entity.LastVerifiedAt);

        if (request.IsActive)
        {
            entity.Activate();
        }
        else
        {
            entity.Deactivate();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(ProviderResponse.FromEntity(entity));
    }
}
