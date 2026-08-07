using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Application.Features.Categories.CreateUserCategory;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Categories.UpdateUserCategory;

/// <summary>Update own user category (5.1.4). Ownership required.</summary>
public sealed record UpdateUserCategoryCommand(
    Guid Id,
    string Name,
    string? Icon = null,
    string? Color = null) : IRequest<Result<UserCategoryResponse>>;

public sealed class UpdateUserCategoryValidator : AbstractValidator<UpdateUserCategoryCommand>
{
    public UpdateUserCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(CreateUserCategoryValidator.NameMaxLength);

        RuleFor(x => x.Icon)
            .MaximumLength(CreateUserCategoryValidator.IconMaxLength)
            .When(x => x.Icon is not null);

        RuleFor(x => x.Color)
            .MaximumLength(CreateUserCategoryValidator.ColorMaxLength)
            .When(x => x.Color is not null);
    }
}

public sealed class UpdateUserCategoryHandler
    : IRequestHandler<UpdateUserCategoryCommand, Result<UserCategoryResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateUserCategoryHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<UserCategoryResponse>> Handle(
        UpdateUserCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<UserCategoryResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var name = request.Name.Trim();

        var entity = await _db.UserCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure<UserCategoryResponse>(DomainErrors.UserCategoryErrors.NotFound);
        }

        if (entity.UserId != userId)
        {
            return Result.Failure<UserCategoryResponse>(DomainErrors.UserCategoryErrors.AccessDenied);
        }

        var duplicate = await _db.UserCategories
            .AsNoTracking()
            .AnyAsync(
                c => c.UserId == userId
                     && c.Id != entity.Id
                     && c.Name.ToLower() == name.ToLower(),
                cancellationToken);

        if (duplicate)
        {
            return Result.Failure<UserCategoryResponse>(DomainErrors.UserCategoryErrors.DuplicateName);
        }

        entity.Update(
            name,
            string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim(),
            string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim());

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UserCategoryResponse(
            Id: entity.Id,
            Name: entity.Name,
            Icon: entity.Icon,
            Color: entity.Color,
            CreatedAt: entity.CreatedAt));
    }
}
