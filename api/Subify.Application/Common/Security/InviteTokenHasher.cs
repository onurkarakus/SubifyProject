using System.Security.Cryptography;
using System.Text;

namespace Subify.Application.Common.Security;

/// <summary>
/// Invite plain token ↔ SHA-256 hex hash (7.2).
/// Only the hash is stored on <c>UserInvite.TokenHash</c>; plain token is response-only.
/// </summary>
public static class InviteTokenHasher
{
    /// <summary>Cryptographically random plain token (URL-safe base64, 32 bytes).</summary>
    public static string GeneratePlainText()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>SHA-256 over UTF-8 plain token, uppercase hex (64 chars).</summary>
    public static string Hash(string plainToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainToken);

        var bytes = Encoding.UTF8.GetBytes(plainToken.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
