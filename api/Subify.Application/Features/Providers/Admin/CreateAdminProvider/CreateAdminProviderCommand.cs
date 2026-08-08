using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Providers.Admin.CreateAdminProvider;

/// <summary>SuperAdmin: create catalog provider (5.2.3).</summary>
public sealed record CreateAdminProviderCommand(
    string Name,
    string Slug,
    string Currency,
    string BillingCycle,
    string Region,
    decimal? Price = null,
    decimal? PriceBefore = null,
    string? SourceUrl = null,
    string? LogoUrl = null) : IRequest<Result<ProviderResponse>>;

public sealed class CreateAdminProviderValidator : AbstractValidator<CreateAdminProviderCommand>
{
    public CreateAdminProviderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase kebab-case (e.g. netflix, disney-plus).");
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10)
            .Must(SupportedCurrencies.IsSupported)
            .WithMessage("Currency must be TRY, USD, EUR, or GBP.");
        RuleFor(x => x.BillingCycle).NotEmpty()
            .Must(CreateSubscriptionValidator.BeSupportedBillingCycle)
            .WithMessage("Billing cycle must be monthly or yearly.");
        RuleFor(x => x.Region).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price is not null);
        RuleFor(x => x.PriceBefore).GreaterThan(0).When(x => x.PriceBefore is not null);
        RuleFor(x => x.SourceUrl).MaximumLength(500).When(x => x.SourceUrl is not null);
        RuleFor(x => x.LogoUrl).MaximumLength(500).When(x => x.LogoUrl is not null);
    }
}

public sealed class CreateAdminProviderHandler
    : IRequestHandler<CreateAdminProviderCommand, Result<ProviderResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateAdminProviderHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ProviderResponse>> Handle(
        CreateAdminProviderCommand request,
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

        var slug = request.Slug.Trim().ToLowerInvariant();
        var name = request.Name.Trim();

        if (await _db.Providers.IgnoreQueryFilters().AnyAsync(p => p.Slug == slug, cancellationToken))
        {
            return Result.Failure<ProviderResponse>(DomainErrors.ProviderErrors.DuplicateSlug);
        }

        if (await _db.Providers.IgnoreQueryFilters()
                .AnyAsync(p => p.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            return Result.Failure<ProviderResponse>(DomainErrors.ProviderErrors.DuplicateName);
        }

        var entity = Provider.CreateCatalog(
            name: name,
            slug: slug,
            currency: request.Currency,
            price: request.Price,
            billingCycle: cycle,
            region: request.Region,
            sourceUrl: string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim(),
            logoUrl: string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim(),
            priceBefore: request.PriceBefore);

        await _db.Providers.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ProviderResponse.FromEntity(entity));
    }
}
