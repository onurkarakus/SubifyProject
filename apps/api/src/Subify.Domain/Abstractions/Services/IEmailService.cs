using System;

namespace Subify.Domain.Abstractions.Services;

public interface IEmailService
{
    public Task<bool> SendVerificationEmailAsync(string to, string verificationToken);

}
