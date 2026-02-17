using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;
using System.Security.Claims;

namespace Subify.Api.Features.UserCategories.DeleteUserCategory;

public class DeleteUserCategoryHandler : IRequestHandler<DeleteUserCategoryCommand, Result>
{
    private readonly SubifyDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteUserCategoryHandler(SubifyDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result> Handle(DeleteUserCategoryCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Result.Failure(DomainErrors.User.UnAuthorized);

        var userCategory = await _context.UserCategories
            .FirstOrDefaultAsync(uc => uc.Id == request.Id && uc.UserId == userId, cancellationToken);

        if (userCategory is null)
        {
            return Result.Failure(DomainErrors.UserCategory.NotFound);
        }

        _context.UserCategories.Remove(userCategory);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
