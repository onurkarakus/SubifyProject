using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Domain.Abstractions.Shared;

public interface IValidationResult
{
    public static readonly Error ValidationError = DomainErrors.ValidationErrors.ValidationFailed;

    Error[] Errors { get; }
}
