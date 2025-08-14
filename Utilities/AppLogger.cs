// <copyright file="AppLogger.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/25/2025</date>
// <summary>Thin Wrapper around Serilog</summary>
using Serilog;
using RollingInterval = Serilog.RollingInterval;
using Microsoft.ApplicationInsights.Extensibility;

namespace GMS.TifoXRCoreWebAPI.Utilities
{
    public static class AppLogger
    {
        public static void Initialize(string appInsightsConnectionString)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/TifoXRCoreWebAPI_log.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.ApplicationInsights(
                new TelemetryConfiguration { ConnectionString = appInsightsConnectionString },
                TelemetryConverter.Traces)
                .CreateLogger();
        }


        public static void Info(string message) => Log.Information(message);
        public static void Warn(string message) => Log.Warning(message);
        public static void Error(Exception ex, string message) => Log.Error(ex, message);
        public static void Debug(string message) => Log.Debug(message);
        public static void Close() => Log.CloseAndFlush();
    }
}
