using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Constants;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Resources.Admin.CreateAdminResource;

/// <summary>SuperAdmin: create i18n resource row (6.3.3).</summary>
public sealed record CreateAdminResourceCommand(
    string PageName,
    string Name,
    string LanguageCode,
    string Value) : IRequest<Result<AdminResourceResponse>>;

public sealed class CreateAdminResourceValidator : AbstractValidator<CreateAdminResourceCommand>
{
    public CreateAdminResourceValidator()
    {
        RuleFor(x => x.PageName).NotEmpty().MaximumLength(ResourceConstants.PageNameMaxLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ResourceConstants.NameMaxLength);
        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(SupportedLocales.IsSupported)
            .WithMessage("Language code must be 'tr' or 'en'.");
        RuleFor(x => x.Value).NotEmpty().MaximumLength(ResourceConstants.ValueMaxLength);
    }
}

public sealed class CreateAdminResourceHandler
    : IRequestHandler<CreateAdminResourceCommand, Result<AdminResourceResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMemoryCache _cache;

    public CreateAdminResourceHandler(
        ISubifyDbContext db,
        ICurrentUserService currentUser,
        IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<Result<AdminResourceResponse>> Handle(
        CreateAdminResourceCommand request,
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

        var page = request.PageName.Trim();
        var name = request.Name.Trim();
        var lang = SupportedLocales.Normalize(request.LanguageCode);
        var value = request.Value.Trim();

        var exists = await _db.Resources.AnyAsync(
            r => r.PageName == page && r.Name == name && r.LanguageCode == lang,
            cancellationToken);

        if (exists)
        {
            return Result.Failure<AdminResourceResponse>(DomainErrors.ResourceErrors.ResourceConflict);
        }

        var entity = Resource.Create(page, name, lang, value);
        _db.Resources.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        ResourceCache.Invalidate(_cache, lang);

        return Result.Success(ToResponse(entity));
    }

    internal static AdminResourceResponse ToResponse(Resource r) =>
        new(r.Id, r.PageName, r.Name, r.LanguageCode, r.Value, r.CreatedAt, r.UpdatedAt);
}
