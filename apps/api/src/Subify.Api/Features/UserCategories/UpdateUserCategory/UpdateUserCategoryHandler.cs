using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Security.Claims;

namespace Subify.Api.Features.UserCategories.UpdateUserCategory;

public class UpdateUserCategoryHandler : IRequestHandler<UpdateUserCategoryCommand, Result>
{
    private readonly SubifyDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateUserCategoryHandler(SubifyDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result> Handle(UpdateUserCategoryCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        var userCategory = await _context.UserCategories
            .FirstOrDefaultAsync(uc => uc.Id == request.Id && uc.UserId == userId, cancellationToken);

        if (userCategory is null)
        {
            return Result.Failure(DomainErrors.UserCategoryErrors.NotFound);
        }

        userCategory.Name = request.Name;
        userCategory.Slug = request.Name.ToLower().Replace(" ", "-");
        userCategory.Icon = request.Icon;
        userCategory.Color = request.Color;
        userCategory.IsActive = request.IsActive;
        userCategory.SortOrder = request.SortOrder;
        userCategory.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
