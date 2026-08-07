using System.Collections;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace Subify.Api.Common.Logging;

/// <summary>
/// 14.1.2 — When structured objects are destructured into logs, mask secret-like properties.
/// Does not rewrite free-form message text; callers must still avoid logging secrets.
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    public const string Redacted = "***REDACTED***";

    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "newpassword",
        "currentpassword",
        "confirmpassword",
        "secret",
        "secretkey",
        "apikey",
        "aiapikey",
        "smtppassword",
        "token",
        "accesstoken",
        "refreshtoken",
        "authorization",
        "connectionstring",
        "jwt",
        "privatekey"
    };

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        result = null!;

        if (value is null || value is string || value.GetType().IsPrimitive || value is decimal)
        {
            return false;
        }

        if (value is IDictionary dictionary)
        {
            var props = new List<LogEventProperty>();
            foreach (DictionaryEntry entry in dictionary)
            {
                var name = entry.Key?.ToString() ?? "_";
                props.Add(new LogEventProperty(
                    name,
                    IsSensitiveName(name)
                        ? new ScalarValue(Redacted)
                        : propertyValueFactory.CreatePropertyValue(entry.Value, destructureObjects: true)));
            }

            result = new StructureValue(props);
            return true;
        }

        var type = value.GetType();
        if (type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true
            && type != typeof(Guid)
            && !type.IsEnum)
        {
            return false;
        }

        // Plain DTOs / anonymous objects with public properties
        var publicProps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

        if (publicProps.Length == 0)
        {
            return false;
        }

        var structure = new List<LogEventProperty>(publicProps.Length);
        foreach (var prop in publicProps)
        {
            object? propValue;
            try
            {
                propValue = prop.GetValue(value);
            }
            catch
            {
                propValue = null;
            }

            structure.Add(new LogEventProperty(
                prop.Name,
                IsSensitiveName(prop.Name)
                    ? new ScalarValue(Redacted)
                    : propertyValueFactory.CreatePropertyValue(propValue, destructureObjects: true)));
        }

        result = new StructureValue(structure, type.Name);
        return true;
    }

    public static bool IsSensitiveName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var n = name.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        if (SensitiveNames.Contains(n))
        {
            return true;
        }

        // Partial matches: "SmtpPassword", "JwtSecretKey", "BearerToken"
        return n.Contains("password", StringComparison.OrdinalIgnoreCase)
               || n.Contains("secret", StringComparison.OrdinalIgnoreCase)
               || n.Contains("apikey", StringComparison.OrdinalIgnoreCase)
               || (n.Contains("token", StringComparison.OrdinalIgnoreCase)
                   && !n.Contains("tokenhash", StringComparison.OrdinalIgnoreCase)
                   && !n.Equals("tokenType", StringComparison.OrdinalIgnoreCase));
    }
}
