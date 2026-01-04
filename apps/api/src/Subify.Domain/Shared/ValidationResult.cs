using Subify.Domain.Abstractions.Shared;

namespace Subify.Domain.Shared;

public class ValidationResult : Result, IValidationResult
{
    private ValidationResult(Error[] errors)
        : base(false, IValidationResult.ValidationError)
    {
        Errors = errors;
    }
    public Error[] Errors { get; }

    public static ValidationResult WithErrors(Error[] errors) => new(errors);
}

public sealed class ValidationResult<T>: Result<T>, IValidationResult
{
    private ValidationResult(Error[] errors)
        : base(default, false, IValidationResult.ValidationError)
    {
        Errors = errors;
    }
    public Error[] Errors { get; }
    public static ValidationResult<T> WithErrors(Error[] errors) => new(errors);
}
