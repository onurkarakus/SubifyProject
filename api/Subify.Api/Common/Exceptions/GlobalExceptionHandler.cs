using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Subify.Domain.Errors;

namespace Subify.Api.Common.Exceptions;

/// <summary>
/// Last-resort handler for unhandled exceptions → RFC 7807 ProblemDetails with <c>SYS_001</c> and <c>traceId</c>.
/// In Development, includes exception message (not full stack) to aid debugging.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string ErrorTypeBaseUri = "https://api.subify.app/errors";

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Client aborted the request — not an application fault
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(
                exception,
                "Request aborted by client. TraceId: {TraceId}",
                httpContext.TraceIdentifier);

            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }

            return true;
        }

        var error = DomainErrors.SystemErrors.InternalServerError;
        var traceId = httpContext.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception. TraceId: {TraceId}, Path: {Path}",
            traceId,
            httpContext.Request.Path.Value);

        var detail = _environment.IsDevelopment()
            ? exception.Message
            : error.Description;

        var extensions = new Dictionary<string, object?>
        {
            ["errorCode"] = error.Code,
            ["traceId"] = traceId
        };

        if (_environment.IsDevelopment())
        {
            extensions["exceptionType"] = exception.GetType().FullName;

            if (exception.InnerException is not null)
            {
                extensions["innerException"] = exception.InnerException.Message;
            }
        }

        await Results.Problem(
            detail: detail,
            instance: httpContext.Request.Path,
            statusCode: StatusCodes.Status500InternalServerError,
            title: error.Title,
            type: $"{ErrorTypeBaseUri}/{error.Code}",
            extensions: extensions).ExecuteAsync(httpContext);

        return true;
    }
}
