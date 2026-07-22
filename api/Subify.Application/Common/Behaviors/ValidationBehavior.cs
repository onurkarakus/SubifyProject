using System.Reflection;
using FluentValidation;
using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Common.Behaviors;

/// <summary>
/// Runs FluentValidation validators before the request handler.
/// On failure, short-circuits with <see cref="ValidationResult"/> / <see cref="ValidationResult{T}"/>
/// so endpoints can map to RFC 7807 ProblemDetails (VAL_*).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToArray();

        if (failures.Length == 0)
        {
            return await next(cancellationToken);
        }

        var errors = failures
            .Select(failure => Error.Validation(
                code: string.IsNullOrWhiteSpace(failure.PropertyName) ? "VAL_001" : failure.PropertyName,
                title: string.IsNullOrWhiteSpace(failure.PropertyName) ? "Validation Failed" : failure.PropertyName,
                description: failure.ErrorMessage))
            .DistinctBy(error => (error.Code, error.Description))
            .ToArray();

        return CreateValidationResponse(errors);
    }

    private static TResponse CreateValidationResponse(Error[] errors)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)ValidationResult.WithErrors(errors);
        }

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(TResponse).GetGenericArguments()[0];
            var validationResultType = typeof(ValidationResult<>).MakeGenericType(resultType);
            var withErrors = validationResultType.GetMethod(
                nameof(ValidationResult<object>.WithErrors),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(Error[])],
                modifiers: null);

            if (withErrors is null)
            {
                throw new InvalidOperationException(
                    $"Could not find WithErrors on {validationResultType.Name}.");
            }

            return (TResponse)withErrors.Invoke(null, [errors])!;
        }

        throw new InvalidOperationException(
            $"ValidationBehavior only supports Result and Result<T> responses. Request '{typeof(TRequest).Name}' returns '{typeof(TResponse).Name}'.");
    }
}
