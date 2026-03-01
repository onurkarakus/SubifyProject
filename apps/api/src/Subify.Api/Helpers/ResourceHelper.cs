using Microsoft.EntityFrameworkCore;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Helpers;

public class ResourceHelper
{
    private readonly SubifyDbContext _dbContext;

    public ResourceHelper(SubifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dictionary<string, string>> GetResourcesForPageAsync(
        string pageName,
        string languageCode,
        CancellationToken cancellationToken)
    {
        var resources = await _dbContext.Resources
            .AsNoTracking()
            .Where(r => r.PageName == pageName && r.LanguageCode == languageCode)
            .ToDictionaryAsync(r => r.Name, r => r.Value, cancellationToken);

        return resources;
    }

    public async Task<Domain.Models.Entities.Common.Resource> GetResourceForPageAndItemAsync(
        string pageName,
        string languageCode,
        string resourceKey,
        CancellationToken cancellationToken)
    {
        var resourceInformation = await _dbContext.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.PageName == pageName && r.LanguageCode == languageCode && r.Name == $"{pageName}_{resourceKey}");

        return resourceInformation;
    }
}
