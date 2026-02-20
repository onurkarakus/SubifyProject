using MediatR;
using Subify.Domain.Errors;
using Subify.Domain.Models.Entities.Subscriptions;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Security.Claims;

namespace Subify.Api.Features.UserCategories.CreateUserCategory;

public class CreateUserCategoryHandler : IRequestHandler<CreateUserCategoryCommand, Result<Guid>>
{
    private readonly SubifyDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateUserCategoryHandler(SubifyDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<Guid>> Handle(CreateUserCategoryCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result.Failure<Guid>(DomainErrors.UserErrors.UnAuthorized);
        }

        var userCategory = new UserCategory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Icon = request.Icon,
            Color = request.Color,
            Slug = request.Name.ToLower().Replace(" ", "-"),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.UserCategories.Add(userCategory);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(userCategory.Id);
    }
}
