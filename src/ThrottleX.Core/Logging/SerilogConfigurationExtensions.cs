using Serilog;
using Serilog.Events;

namespace ThrottleX.Core.Logging;

public static class SerilogConfigurationExtensions
{
    public static LoggerConfiguration ReadFromLoggingConfiguration(this LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        var loggingSection = configuration.GetSection("Logging:LogLevel");
        if (!loggingSection.Exists())
        {
            return loggerConfiguration;
        }

        foreach (var (key, value) in GetLogLevelEntries(loggingSection))
        {
            if (!TryParseLogLevel(value, out var level)) continue;

            if (string.Equals(key, "Default", StringComparison.OrdinalIgnoreCase))
            {
                loggerConfiguration.MinimumLevel.Is(level);
            }
            else
            {
                loggerConfiguration.MinimumLevel.Override(key, level);
            }
        }

        return loggerConfiguration;
    }

    private static IEnumerable<(string Key, string Value)> GetLogLevelEntries(IConfigurationSection section, string prefix = "")
    {
        foreach (var child in section.GetChildren())
        {
            var key = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}.{child.Key}";
            if (child.Value != null)
            {
                yield return (key, child.Value);
            }
            else
            {
                foreach (var entry in GetLogLevelEntries(child, key))
                {
                    yield return entry;
                }
            }
        }
    }

    private static bool TryParseLogLevel(string? value, out LogEventLevel level)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            level = LogEventLevel.Information;
            return false;
        }

        if (Enum.TryParse<LogLevel>(value, true, out var msLogLevel))
        {
            level = msLogLevel switch
            {
                LogLevel.Trace => LogEventLevel.Verbose,
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Critical => LogEventLevel.Fatal,
                LogLevel.None => (LogEventLevel)1000,
                _ => LogEventLevel.Information
            };
            return true;
        }

        if (Enum.TryParse(value, true, out level))
        {
            return true;
        }

        level = LogEventLevel.Information;

        return false;
    }
}
