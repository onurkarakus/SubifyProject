namespace Subify.Api.Common.Extensions;

using Subify.Domain.Abstractions.Shared; // For IValidationResult
using Subify.Domain.Shared;
using Microsoft.AspNetCore.Http; // For StatusCodes and IResult
using Microsoft.AspNetCore.Mvc; // For ProblemDetails
using System;
using System.Collections.Generic;
using System.Linq; // For GroupBy, ToDictionary
using Subify.Domain.Errors; // Added this

public static class ResultExtensions
{
    // Converts an Error object to ProblemDetails
    public static ProblemDetails ToProblemDetails(this Error error)
    {
        return new ProblemDetails
        {
            Status = GetStatusCode(error.Type),
            Title = error.Title,
            Type = $"https://api.subify.app/errors/{error.Code}",
            Detail = error.Description,
            Extensions = new Dictionary<string, object?>
            {
                { "errorCode", error.Code }
            }
        };
    }

    // Converts a failed Result<T> to ProblemDetails, including validation errors if applicable
    public static ProblemDetails ToProblemDetails<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Successful result cannot be converted to problem details.");
        }

        var problemDetails = result.Error.ToProblemDetails(); // Start with the base error's problem details

        if (result is IValidationResult validationResult)
        {
            problemDetails.Extensions["errors"] = validationResult.Errors
                .GroupBy(e => e.Code) // Group by error code as a pseudo-field
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(e => e.Description).ToArray()
                );

            // Update Detail to match ERROR_CODES.md example for validation failures
            if (problemDetails.Detail == DomainErrors.ValidationErrors.ValidationFailed.Description)
            {
                 problemDetails.Detail = "One or more validation errors occurred.";
            }
        }
        return problemDetails;
    }

    // Converts a failed Result to ProblemDetails, including validation errors if applicable
    public static ProblemDetails ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Successful result cannot be converted to problem details.");
        }

        var problemDetails = result.Error.ToProblemDetails(); // Start with the base error's problem details

        if (result is IValidationResult validationResult)
        {
            problemDetails.Extensions["errors"] = validationResult.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(e => e.Description).ToArray()
                );
            // Update Detail to match ERROR_CODES.md example for validation failures
            if (problemDetails.Detail == DomainErrors.ValidationErrors.ValidationFailed.Description)
            {
                 problemDetails.Detail = "One or more validation errors occurred.";
            }
        }
        return problemDetails;
    }

    public static IResult MapResult<T>(
        this Result<T> result,
        Func<T, IResult> onSuccess,
        Func<Result<T>, IResult> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result);
    }

    // Also need to adapt MapResult for non-generic Result
    public static IResult MapResult(
        this Result result,
        Func<IResult> onSuccess,
        Func<Result, IResult> onFailure)
    {
        return result.IsSuccess ? onSuccess() : onFailure(result);
    }

    private static int GetStatusCode(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Locked => StatusCodes.Status423Locked,
            ErrorType.TooManyRequest => StatusCodes.Status429TooManyRequests,
            ErrorType.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
            ErrorType.InternalServerError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetTitle(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => "Validation Error",
            ErrorType.NotFound => "Resource Not Found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.Locked => "Locked",
            ErrorType.TooManyRequest => "Too Many Requests",
            ErrorType.ServiceUnavailable => "Service Unavailable",
            ErrorType.InternalServerError => "Internal Server Error",
            _ => "An error occurred"
        };
}