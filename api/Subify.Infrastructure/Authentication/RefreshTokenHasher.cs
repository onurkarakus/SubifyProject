using System.Security.Cryptography;
using System.Text;

namespace Subify.Infrastructure.Authentication;

/// <summary>
/// Refresh token plain ↔ SHA-256 hex hash (task 3.1.2).
/// Only the hash is stored in <c>RefreshTokens.TokenHash</c>; plain text is response-only.
/// </summary>
public static class RefreshTokenHasher
{
    /// <summary>Cryptographically random plain token (Base64, 32 bytes → 44 chars).</summary>
    public static string GeneratePlainText()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// SHA-256 over UTF-8 plain token, uppercase hex (64 chars).
    /// Lookup must use the same algorithm on the client-provided plain token.
    /// </summary>
    public static string Hash(string plainToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainToken);

        var bytes = Encoding.UTF8.GetBytes(plainToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>Constant-time equality for hex hashes (when both sides are already hashed).</summary>
    public static bool FixedTimeEquals(string hashA, string hashB)
    {
        if (string.IsNullOrEmpty(hashA) || string.IsNullOrEmpty(hashB))
        {
            return false;
        }

        var a = Encoding.UTF8.GetBytes(hashA);
        var b = Encoding.UTF8.GetBytes(hashB);
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
