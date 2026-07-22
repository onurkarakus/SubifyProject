using Microsoft.AspNetCore.Mvc;
using Subify.Domain.Abstractions.Shared;
using Subify.Domain.Shared;

namespace Subify.Api.Common.Extensions;

public static class ResultExtensions
{
    private const string ErrorTypeBaseUri = "https://api.subify.app/errors";

    public static ProblemDetails ToProblemDetails(this Error error)
    {
        return new ProblemDetails
        {
            Status = ProblemDetailsStatusMapper.ToStatusCode(error),
            Title = error.Title,
            Type = $"{ErrorTypeBaseUri}/{error.Code}",
            Detail = error.Description,
            Extensions =
            {
                ["errorCode"] = error.Code
            }
        };
    }

    public static ProblemDetails ToProblemDetails<T>(this Result<T> result) =>
        ToProblemDetails((Result)result);

    public static ProblemDetails ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Successful result cannot be converted to problem details.");
        }

        if (result is IValidationResult validationResult)
        {
            return CreateValidationProblemDetails(validationResult, result);
        }

        var problemDetails = result.Error.ToProblemDetails();

        if (result.Errors is { Length: > 1 })
        {
            problemDetails.Extensions["errors"] = result.Errors
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Code) ? "_" : e.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(e => e.Description).ToArray());
        }

        // Always align Status with Error.Type (never leave null / wrong)
        problemDetails.Status = ProblemDetailsStatusMapper.ToStatusCode(result.Error);

        return problemDetails;
    }

    /// <summary>
    /// Maps a failed <see cref="Result"/> to an RFC 7807 ProblemDetails HTTP response
    /// with the correct status code for the domain <see cref="ErrorType"/>.
    /// </summary>
    public static IResult ToFailureHttpResult(this Result result, string? instance = null)
    {
        var problem = result.ToProblemDetails();
        if (!string.IsNullOrWhiteSpace(instance))
        {
            problem.Instance = instance;
        }

        return ToProblemHttpResult(problem);
    }

    public static IResult ToFailureHttpResult<T>(this Result<T> result, string? instance = null) =>
        ToFailureHttpResult((Result)result, instance);

    public static IResult MapResult<T>(
        this Result<T> result,
        Func<T, IResult> onSuccess,
        string? instance = null)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : result.ToFailureHttpResult(instance);
    }

    public static IResult MapResult(
        this Result result,
        Func<IResult> onSuccess,
        string? instance = null)
    {
        return result.IsSuccess
            ? onSuccess()
            : result.ToFailureHttpResult(instance);
    }

    /// <summary>
    /// Legacy overload kept for call sites that supply a custom onFailure mapper.
    /// Prefer <see cref="ToFailureHttpResult(Result, string?)"/> so status codes stay consistent.
    /// </summary>
    public static IResult MapResult<T>(
        this Result<T> result,
        Func<T, IResult> onSuccess,
        Func<Result<T>, IResult> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result);
    }

    public static IResult MapResult(
        this Result result,
        Func<IResult> onSuccess,
        Func<Result, IResult> onFailure)
    {
        return result.IsSuccess ? onSuccess() : onFailure(result);
    }

    public static IResult ToProblemHttpResult(ProblemDetails problem)
    {
        var statusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        problem.Status = statusCode;

        return Results.Problem(
            detail: problem.Detail,
            instance: problem.Instance,
            statusCode: statusCode,
            title: problem.Title,
            type: problem.Type,
            extensions: problem.Extensions);
    }

    private static ProblemDetails CreateValidationProblemDetails(
        IValidationResult validationResult,
        Result result)
    {
        var validationError = IValidationResult.ValidationError;

        var fieldErrors = validationResult.Errors
            .Where(e => !string.Equals(e.Code, validationError.Code, StringComparison.Ordinal))
            .ToArray();

        if (fieldErrors.Length == 0)
        {
            fieldErrors = validationResult.Errors is { Length: > 0 }
                ? validationResult.Errors
                : [result.Error];
        }

        var errorsByField = fieldErrors
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Code) ? "_" : e.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(e => e.Description).ToArray());

        return new ProblemDetails
        {
            Status = ProblemDetailsStatusMapper.ToStatusCode(ErrorType.Validation),
            Title = validationError.Title,
            Type = $"{ErrorTypeBaseUri}/{validationError.Code}",
            Detail = "One or more validation errors occurred.",
            Extensions =
            {
                ["errorCode"] = validationError.Code,
                ["errors"] = errorsByField
            }
        };
    }
}
