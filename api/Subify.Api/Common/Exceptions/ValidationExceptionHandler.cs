using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Subify.Domain.Errors;

namespace Subify.Api.Common.Exceptions;

/// <summary>
/// Maps thrown <see cref="ValidationException"/> (FluentValidation) to RFC 7807 ProblemDetails (HTTP 400).
/// Pipeline <c>ValidationBehavior</c> normally returns <c>Result</c> without throwing; this covers throw paths.
/// </summary>
public sealed class ValidationExceptionHandler : IExceptionHandler
{
    private const string ErrorTypeBaseUri = "https://api.subify.app/errors";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var validationError = DomainErrors.ValidationErrors.ValidationFailed;

        var errorsByField = validationException.Errors
            .GroupBy(failure => string.IsNullOrWhiteSpace(failure.PropertyName) ? "_" : failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = validationError.Title,
            Type = $"{ErrorTypeBaseUri}/{validationError.Code}",
            Detail = "One or more validation errors occurred.",
            Instance = httpContext.Request.Path
        };

        problem.Extensions["errorCode"] = validationError.Code;
        problem.Extensions["errors"] = errorsByField;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        await Results.Problem(
            detail: problem.Detail,
            instance: problem.Instance,
            statusCode: StatusCodes.Status400BadRequest,
            title: problem.Title,
            type: problem.Type,
            extensions: problem.Extensions).ExecuteAsync(httpContext);

        return true;
    }
}
