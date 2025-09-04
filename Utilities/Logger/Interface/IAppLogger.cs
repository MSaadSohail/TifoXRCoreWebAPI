// <copyright file="IAppLogger.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/19/2025</date>
// <summary>Thin Wrapper around Serilog</summary>
// Thin wrapper over Serilog to keep old call sites working.
// IMPORTANT: Do NOT configure Serilog here. Host config lives in Program.cs + appsettings.

namespace GMS.TifoXRCoreWebAPI.Utilities.Logger.Interface
{
    public interface IAppLogger<T>
    {
        bool IsEnabled(LogLevel level);
        void Log(LogLevel level, EventId eventId, Exception? ex, string messageTemplate, params object?[] args);

        // Convenience methods
        void Trace(string messageTemplate, params object?[] args) => Log(LogLevel.Trace, default, null, messageTemplate, args);
        void Debug(string messageTemplate, params object?[] args) => Log(LogLevel.Debug, default, null, messageTemplate, args);
        void Info(string messageTemplate, params object?[] args) => Log(LogLevel.Information, default, null, messageTemplate, args);
        void Warn(string messageTemplate, params object?[] args) => Log(LogLevel.Warning, default, null, messageTemplate, args);
        void Error(string messageTemplate, params object?[] args) => Log(LogLevel.Error, default, null, messageTemplate, args);
        void Error(Exception ex, string messageTemplate, params object?[] args) => Log(LogLevel.Error, default, ex, messageTemplate, args);
        void Critical(Exception ex, string messageTemplate, params object?[] args) => Log(LogLevel.Critical, default, ex, messageTemplate, args);

        // Context (scope) helpers
        IDisposable BeginScope<TState>(TState state) where TState : notnull;

        /// <summary>Begin a scope from an anonymous object or dictionary, e.g. new { CorrelationId = "...", Outcome="Failed" }.</summary>
        IDisposable WithProperties(object state);
    }
}


