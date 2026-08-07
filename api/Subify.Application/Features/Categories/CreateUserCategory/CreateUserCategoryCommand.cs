using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Entities;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Categories.CreateUserCategory;

/// <summary>Create a personal category for the current user (5.1.3).</summary>
public sealed record CreateUserCategoryCommand(
    string Name,
    string? Icon = null,
    string? Color = null) : IRequest<Result<UserCategoryResponse>>;

public sealed class CreateUserCategoryValidator : AbstractValidator<CreateUserCategoryCommand>
{
    public const int NameMaxLength = 100;
    public const int IconMaxLength = 50;
    public const int ColorMaxLength = 10;

    public CreateUserCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(NameMaxLength);

        RuleFor(x => x.Icon)
            .MaximumLength(IconMaxLength)
            .When(x => x.Icon is not null);

        RuleFor(x => x.Color)
            .MaximumLength(ColorMaxLength)
            .When(x => x.Color is not null);
    }
}

public sealed class CreateUserCategoryHandler
    : IRequestHandler<CreateUserCategoryCommand, Result<UserCategoryResponse>>
{
    private readonly ISubifyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateUserCategoryHandler(ISubifyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<UserCategoryResponse>> Handle(
        CreateUserCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return Result.Failure<UserCategoryResponse>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userId = _currentUser.UserId.Value;
        var name = request.Name.Trim();

        var duplicate = await _db.UserCategories
            .AsNoTracking()
            .AnyAsync(
                c => c.UserId == userId
                     && c.Name.ToLower() == name.ToLower(),
                cancellationToken);

        if (duplicate)
        {
            return Result.Failure<UserCategoryResponse>(DomainErrors.UserCategoryErrors.DuplicateName);
        }

        var entity = UserCategory.CreateForUser(
            userId,
            name,
            string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim(),
            string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim());

        await _db.UserCategories.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UserCategoryResponse(
            Id: entity.Id,
            Name: entity.Name,
            Icon: entity.Icon,
            Color: entity.Color,
            CreatedAt: entity.CreatedAt));
    }
}
