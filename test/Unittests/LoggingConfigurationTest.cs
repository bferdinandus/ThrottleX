using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using ThrottleX.Core.Logging;
using Xunit;

namespace Unittests;

public class LoggingConfigurationTest
{
    [Fact]
    public void TestLoggingSectionApplied()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Logging:LogLevel:Default", "Information" },
            { "Logging:LogLevel:Microsoft.AspNetCore", "Warning" },
            { "Logging:LogLevel:ThrottleX.Core.Loconet", "Debug" }
        });
        var configuration = configBuilder.Build();

        var loggerConfig = new LoggerConfiguration()
            .ReadFromLoggingConfiguration(configuration);
        var logger = loggerConfig.CreateLogger();

        Assert.True(logger.IsEnabled(LogEventLevel.Information));
        Assert.False(logger.IsEnabled(LogEventLevel.Debug));

        var msLogger = logger.ForContext("SourceContext", "Microsoft.AspNetCore.Hosting.Diagnostics");
        Assert.True(msLogger.IsEnabled(LogEventLevel.Warning));
        Assert.False(msLogger.IsEnabled(LogEventLevel.Information));

        var loconetLogger = logger.ForContext("SourceContext", "ThrottleX.Core.Loconet.LoconetService");
        Assert.True(loconetLogger.IsEnabled(LogEventLevel.Debug));
        Assert.False(loconetLogger.IsEnabled(LogEventLevel.Verbose));
    }

    [Fact]
    public void TestClassSpecificLogLevelApplied()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Logging:LogLevel:Default", "Warning" },
            { "Logging:LogLevel:ThrottleX.Core.LocoTable.LocoRowImpl", "Trace" },
            { "Logging:LogLevel:WiThrottle.CustomTcpClient", "Error" }
        });
        var configuration = configBuilder.Build();

        var loggerConfig = new LoggerConfiguration()
            .ReadFromLoggingConfiguration(configuration);
        var logger = loggerConfig.CreateLogger();

        var locoRowLogger = logger.ForContext("SourceContext", "ThrottleX.Core.LocoTable.LocoRowImpl");
        Assert.True(locoRowLogger.IsEnabled(LogEventLevel.Verbose));

        var defaultLogger = logger.ForContext("SourceContext", "ThrottleX.Core.SystemHealth.SystemHealthService");
        Assert.True(defaultLogger.IsEnabled(LogEventLevel.Warning));
        Assert.False(defaultLogger.IsEnabled(LogEventLevel.Information));

        var tcpLogger = logger.ForContext("SourceContext", "WiThrottle.CustomTcpClient");
        Assert.True(tcpLogger.IsEnabled(LogEventLevel.Error));
        Assert.False(tcpLogger.IsEnabled(LogEventLevel.Warning));
    }

    [Fact]
    public void TestNestedLoggingSectionApplied()
    {
        var json = """
        {
            "Logging": {
                "LogLevel": {
                    "Default": "Warning",
                    "ThrottleX": {
                        "Core": {
                            "Loconet": "Trace"
                        }
                    }
                }
            }
        }
        """;
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonStream(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
        var configuration = configBuilder.Build();

        var loggerConfig = new LoggerConfiguration()
            .ReadFromLoggingConfiguration(configuration);
        var logger = loggerConfig.CreateLogger();

        Assert.True(logger.IsEnabled(LogEventLevel.Warning));
        Assert.False(logger.IsEnabled(LogEventLevel.Information));

        var loconetLogger = logger.ForContext("SourceContext", "ThrottleX.Core.Loconet.LoconetService");
        Assert.True(loconetLogger.IsEnabled(LogEventLevel.Verbose));
    }

    [Fact]
    public void TestLogLevelNoneDisablesLogging()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Logging:LogLevel:Default", "Information" },
            { "Logging:LogLevel:Microsoft.AspNetCore", "None" }
        });
        var configuration = configBuilder.Build();

        var loggerConfig = new LoggerConfiguration()
            .ReadFromLoggingConfiguration(configuration);
        var logger = loggerConfig.CreateLogger();

        var msLogger = logger.ForContext("SourceContext", "Microsoft.AspNetCore.Hosting");
        Assert.False(msLogger.IsEnabled(LogEventLevel.Fatal));
    }
}
