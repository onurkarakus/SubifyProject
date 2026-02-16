using MediatR;
using Microsoft.EntityFrameworkCore;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Features.Categories.DeleteCategory;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly SubifyDbContext _context;

    public DeleteCategoryHandler(SubifyDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure(DomainErrors.Category.NotFound);
        }

        var isUsed = await _context.Subscriptions.AnyAsync(s => s.CategoryId == request.Id);

        if (isUsed)
        {
            return Result.Failure(DomainErrors.Category.HasActiveSubscriptions);
        }

        category.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}