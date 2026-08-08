using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Resources.Admin.CreateAdminResource;
using Subify.Domain.Constants;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Resources.Admin.UpdateAdminResource;

/// <summary>SuperAdmin: update i18n resource row (6.3.3). Invalidates language pack cache.</summary>
public sealed record UpdateAdminResourceCommand(
    Guid Id,
    string PageName,
    string Name,
    string LanguageCode,
    string Value) : IRequest<Result<AdminResourceResponse>>;

public sealed class UpdateAdminResourceValidator : AbstractValidator<UpdateAdminResourceCommand>
{
    public UpdateAdminResourceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PageName).NotEmpty().MaximumLength(ResourceConstants.PageNameMaxLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ResourceConstants.NameMaxLength);
        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(SupportedLocales.IsSupported)
            .WithMessage("Language code must be 'tr' or 'en'.");
        RuleFor(x => x.Value).NotEmpty().MaximumLength(ResourceConstants.ValueMaxLength);
    }
}

public sealed class UpdateAdminResourceHandler
    : IRequestHandler<UpdateAdminResourceCommand, Result<AdminResourceResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMemoryCache _cache;

    public UpdateAdminResourceHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<Result<AdminResourceResponse>> Handle(
        UpdateAdminResourceCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            return Result.Failure<AdminResourceResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        if (!SupportedLocales.IsSupported(request.LanguageCode))
        {
            return Result.Failure<AdminResourceResponse>(DomainErrors.ResourceErrors.InvalidLanguage);
        }

        var entity = await _db.Resources.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<AdminResourceResponse>(DomainErrors.ResourceErrors.ResourceNotFound);
        }

        var page = request.PageName.Trim();
        var name = request.Name.Trim();
        var lang = SupportedLocales.Normalize(request.LanguageCode);
        var value = request.Value.Trim();
        var previousLang = entity.LanguageCode;

        var conflict = await _db.Resources.AnyAsync(
            r => r.Id != request.Id
                 && r.PageName == page
                 && r.Name == name
                 && r.LanguageCode == lang,
            cancellationToken);

        if (conflict)
        {
            return Result.Failure<AdminResourceResponse>(DomainErrors.ResourceErrors.ResourceConflict);
        }

        entity.Update(page, name, lang, value);
        await _db.SaveChangesAsync(cancellationToken);

        ResourceCache.Invalidate(_cache, previousLang);
        if (!string.Equals(previousLang, lang, StringComparison.OrdinalIgnoreCase))
        {
            ResourceCache.Invalidate(_cache, lang);
        }

        return Result.Success(CreateAdminResourceHandler.ToResponse(entity));
    }
}
