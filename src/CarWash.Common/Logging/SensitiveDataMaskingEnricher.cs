namespace CarWash.Common.Logging;

using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Serilog enricher that sanitizes sensitive properties (passwords, tokens, API keys)
/// before log events are written to any sink.
/// </summary>
public class SensitiveDataMaskingEnricher : ILogEventEnricher
{
    private static readonly string[] SensitiveKeys =
    [
        "password",
        "token",
        "apikey",
        "api-key",
        "secret",
        "authorization",
        "credential"
    ];

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var propertiesToSanitize = logEvent.Properties
            .Where(p => IsSensitive(p.Key))
            .ToList();

        foreach (var property in propertiesToSanitize)
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, new ScalarValue("[REDACTED]")));
        }
    }

    private static bool IsSensitive(string propertyName)
    {
        foreach (var key in SensitiveKeys)
        {
            if (propertyName.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
