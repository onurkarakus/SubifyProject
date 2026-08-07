using System.Text.RegularExpressions;

namespace Subify.Application.Common.Email;

/// <summary>Replaces <c>{{Token}}</c> placeholders in email subjects/bodies.</summary>
public static partial class EmailTemplateRenderer
{
    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    public static string Render(string template, IReadOnlyDictionary<string, string> tokens)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        return TokenRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (tokens.TryGetValue(key, out var value))
            {
                return value;
            }

            foreach (var kv in tokens)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Value;
                }
            }

            return match.Value;
        });
    }
}
