using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Subscriptions.CreateSubscription;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Providers.Admin.ImportAdminProviders;

/// <summary>
/// SuperAdmin bulk catalog import (16.6.3).
/// Default: insert missing by slug; optional updateExisting overwrites fields.
/// </summary>
public sealed record ImportAdminProvidersCommand(
    IReadOnlyList<ImportProviderItem> Providers,
    bool UpdateExisting = false) : IRequest<Result<ImportAdminProvidersResponse>>;

public sealed record ImportProviderItem(
    string Name,
    string Slug,
    string Currency,
    string BillingCycle,
    string Region,
    decimal? Price = null,
    decimal? PriceBefore = null,
    string? SourceUrl = null,
    string? LogoUrl = null);

public sealed record ImportAdminProvidersResponse(
    int Created,
    int Updated,
    int Skipped,
    int Failed,
    IReadOnlyList<ImportProviderResultRow> Results);

public sealed record ImportProviderResultRow(
    string Slug,
    string Status,
    string? Message = null);

public sealed class ImportAdminProvidersValidator : AbstractValidator<ImportAdminProvidersCommand>
{
    public ImportAdminProvidersValidator()
    {
        RuleFor(x => x.Providers)
            .NotNull()
            .Must(p => p.Count is > 0 and <= 200)
            .WithMessage("Provide 1–200 providers.");

        RuleForEach(x => x.Providers).ChildRules(item =>
        {
            item.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            item.RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
                .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .WithMessage("Slug must be lowercase kebab-case.");
            item.RuleFor(x => x.Currency).NotEmpty().MaximumLength(10)
                .Must(SupportedCurrencies.IsSupported)
                .WithMessage("Currency must be TRY, USD, EUR, or GBP.");
            item.RuleFor(x => x.BillingCycle).NotEmpty()
                .Must(CreateSubscriptionValidator.BeSupportedBillingCycle)
                .WithMessage("Billing cycle must be monthly or yearly.");
            item.RuleFor(x => x.Region).NotEmpty().MaximumLength(10);
            item.RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price is not null);
            item.RuleFor(x => x.PriceBefore).GreaterThan(0).When(x => x.PriceBefore is not null);
            item.RuleFor(x => x.SourceUrl).MaximumLength(500).When(x => x.SourceUrl is not null);
            item.RuleFor(x => x.LogoUrl).MaximumLength(500).When(x => x.LogoUrl is not null);
        });
    }
}

public sealed class ImportAdminProvidersHandler
    : IRequestHandler<ImportAdminProvidersCommand, Result<ImportAdminProvidersResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ImportAdminProvidersHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ImportAdminProvidersResponse>> Handle(
        ImportAdminProvidersCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<ImportAdminProvidersResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var failed = 0;
        var rows = new List<ImportProviderResultRow>(request.Providers.Count);
        var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Providers)
        {
            var slug = item.Slug.Trim().ToLowerInvariant();
            if (!seenSlugs.Add(slug))
            {
                failed++;
                rows.Add(new ImportProviderResultRow(slug, "failed", "Duplicate slug in payload."));
                continue;
            }

            if (!CreateSubscriptionValidator.TryParseBillingCycle(item.BillingCycle, out var cycle))
            {
                failed++;
                rows.Add(new ImportProviderResultRow(slug, "failed", "Invalid billing cycle."));
                continue;
            }

            if (!SupportedCurrencies.IsSupported(item.Currency))
            {
                failed++;
                rows.Add(new ImportProviderResultRow(slug, "failed", "Invalid currency."));
                continue;
            }

            var existing = await _db.Providers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

            if (existing is null)
            {
                var name = item.Name.Trim();
                var nameClash = await _db.Providers
                    .IgnoreQueryFilters()
                    .AnyAsync(p => p.Name.ToLower() == name.ToLower(), cancellationToken);
                if (nameClash)
                {
                    failed++;
                    rows.Add(new ImportProviderResultRow(slug, "failed", "Name already used."));
                    continue;
                }

                var entity = Provider.CreateCatalog(
                    name: name,
                    slug: slug,
                    currency: item.Currency,
                    price: item.Price,
                    billingCycle: cycle,
                    region: item.Region,
                    sourceUrl: string.IsNullOrWhiteSpace(item.SourceUrl) ? null : item.SourceUrl.Trim(),
                    logoUrl: string.IsNullOrWhiteSpace(item.LogoUrl) ? null : item.LogoUrl.Trim(),
                    priceBefore: item.PriceBefore);

                await _db.Providers.AddAsync(entity, cancellationToken);
                created++;
                rows.Add(new ImportProviderResultRow(slug, "created"));
                continue;
            }

            if (!request.UpdateExisting)
            {
                skipped++;
                rows.Add(new ImportProviderResultRow(slug, "skipped", "Already exists."));
                continue;
            }

            var newName = item.Name.Trim();
            var nameTaken = await _db.Providers
                .IgnoreQueryFilters()
                .AnyAsync(
                    p => p.Id != existing.Id && p.Name.ToLower() == newName.ToLower(),
                    cancellationToken);
            if (nameTaken)
            {
                failed++;
                rows.Add(new ImportProviderResultRow(slug, "failed", "Name already used by another provider."));
                continue;
            }

            existing.Update(
                name: newName,
                slug: slug,
                logoUrl: string.IsNullOrWhiteSpace(item.LogoUrl) ? null : item.LogoUrl.Trim(),
                currency: SupportedCurrencies.Normalize(item.Currency),
                price: item.Price,
                priceBefore: item.PriceBefore,
                billingCycle: cycle,
                region: item.Region.Trim().ToUpperInvariant(),
                sourceUrl: string.IsNullOrWhiteSpace(item.SourceUrl) ? null : item.SourceUrl.Trim(),
                lastVerifiedAt: DateTimeOffset.UtcNow);

            if (!existing.IsActive)
            {
                existing.Activate();
            }

            updated++;
            rows.Add(new ImportProviderResultRow(slug, "updated"));
        }

        if (created > 0 || updated > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new ImportAdminProvidersResponse(
            Created: created,
            Updated: updated,
            Skipped: skipped,
            Failed: failed,
            Results: rows));
    }
}
