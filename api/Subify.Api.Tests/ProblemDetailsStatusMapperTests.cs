using Microsoft.AspNetCore.Http;
using Subify.Api.Common.Extensions;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Api.Tests;

public class ProblemDetailsStatusMapperTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Failure, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Locked, StatusCodes.Status423Locked)]
    [InlineData(ErrorType.TooManyRequest, StatusCodes.Status429TooManyRequests)]
    [InlineData(ErrorType.InternalServerError, StatusCodes.Status500InternalServerError)]
    [InlineData(ErrorType.ServiceUnavailable, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(ErrorType.GatewayTimeout, StatusCodes.Status504GatewayTimeout)]
    public void ToStatusCode_maps_each_ErrorType(ErrorType errorType, int expectedStatus)
    {
        Assert.Equal(expectedStatus, ProblemDetailsStatusMapper.ToStatusCode(errorType));
    }

    [Fact]
    public void AllMappings_covers_every_ErrorType_except_None()
    {
        var expectedTypes = Enum.GetValues<ErrorType>()
            .Where(t => t != ErrorType.None)
            .OrderBy(t => t)
            .ToArray();

        var actualTypes = ProblemDetailsStatusMapper.AllMappings.Keys
            .OrderBy(t => t)
            .ToArray();

        Assert.Equal(expectedTypes, actualTypes);
    }

    [Fact]
    public void DomainErrors_sample_codes_map_to_expected_status()
    {
        Assert.Equal(400, DomainErrors.ValidationErrors.ValidationFailed.ToProblemDetails().Status);
        Assert.Equal(401, DomainErrors.Auth.InvalidCredentials.ToProblemDetails().Status);
        Assert.Equal(403, DomainErrors.Subscription.SubscriptionAccessDenied.ToProblemDetails().Status);
        Assert.Equal(404, DomainErrors.Subscription.SubscriptionNotFound.ToProblemDetails().Status);
        Assert.Equal(409, DomainErrors.Auth.EmailAlreadyRegistered.ToProblemDetails().Status);
        Assert.Equal(423, DomainErrors.Auth.AccountLocked.ToProblemDetails().Status);
        Assert.Equal(429, DomainErrors.SystemErrors.TooManyRequests.ToProblemDetails().Status);
        Assert.Equal(500, DomainErrors.SystemErrors.InternalServerError.ToProblemDetails().Status);
        Assert.Equal(503, DomainErrors.SystemErrors.ServiceUnavailable.ToProblemDetails().Status);
        Assert.Equal(503, DomainErrors.AiErrors.ApiKeyMissing.ToProblemDetails().Status);
        Assert.Equal(504, DomainErrors.SystemErrors.GatewayTimeout.ToProblemDetails().Status);
    }

    [Fact]
    public void ToFailureHttpResult_uses_mapped_status_code()
    {
        var result = Result.Failure(DomainErrors.Auth.InvalidCredentials);
        var httpResult = result.ToFailureHttpResult("/api/auth/login");

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        var statusResult = (IStatusCodeHttpResult)httpResult;
        Assert.Equal(StatusCodes.Status401Unauthorized, statusResult.StatusCode);
    }
}
