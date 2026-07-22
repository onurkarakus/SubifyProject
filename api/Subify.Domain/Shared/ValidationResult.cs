using Subify.Domain.Abstractions.Shared;

namespace Subify.Domain.Shared;

public class ValidationResult : Result, IValidationResult
{
    private ValidationResult(Error[] errors)
        : base(false, IValidationResult.ValidationError, errors)
    {
    }

    public static ValidationResult WithErrors(Error[] errors) => new(errors);
}

public sealed class ValidationResult<T> : Result<T>, IValidationResult
{
    private ValidationResult(Error[] errors)
        : base(default, false, IValidationResult.ValidationError, errors)
    {
    }

    public static ValidationResult<T> WithErrors(Error[] errors) => new(errors);
}