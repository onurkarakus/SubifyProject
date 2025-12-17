namespace Subify.Domain.Models.RequestEntities.Auth;

public record RegisterRequest()
{
    public string Email { get; init; }

    public string Password { get; init; }

    public string FullName { get; init; }  
};
