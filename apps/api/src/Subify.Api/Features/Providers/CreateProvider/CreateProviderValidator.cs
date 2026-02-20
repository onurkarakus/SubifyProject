using FluentValidation;

namespace Subify.Api.Features.Providers.CreateProvider;

public class CreateProviderValidator: AbstractValidator<CreateProviderCommand>
{
    public CreateProviderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Website).Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("Please enter a valid website.");
    }
}
