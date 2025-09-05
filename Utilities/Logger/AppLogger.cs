// <copyright file="AppLogger.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/25/2025</date>
// <summary>Thin Wrapper around Serilog</summary>
// Thin wrapper over Serilog to keep old call sites working.
// IMPORTANT: Do NOT configure Serilog here. Host config lives in Program.cs + appsettings.
// <summary>Thin wrapper over ILogger<T> with structured-scope helpers</summary>

using GMS.TifoXRCoreWebAPI.Utilities.Logger.Interface;

namespace GMS.TifoXRCoreWebAPI.Utilities.Logger
{
    internal sealed class AppLogger<T> : IAppLogger<T>
    {
        private readonly ILogger<T> _logger;
        public AppLogger(ILogger<T> logger) => _logger = logger;

        public bool IsEnabled(LogLevel level) => _logger.IsEnabled(level);

        public void Log(LogLevel level, EventId eventId, Exception? ex, string messageTemplate, params object?[] args)
            => _logger.Log(level, eventId, ex, messageTemplate, args);

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => _logger.BeginScope(state);

        /// <summary>
        /// Create a scope from key/value pairs without reflection (preferred in hot paths).
        /// Usage: using (_log.WithProperties(("SpaceId", spaceId), ("BoothId", boothId))) { ... }
        /// </summary>
        public IDisposable WithProperties(params (string Key, object? Value)[] properties)
        {
            var dict = new Dictionary<string, object?>(properties.Length);
            foreach (var (k, v) in properties) dict[k] = v;
            return _logger.BeginScope(dict);
        }

        /// <summary>
        /// Convenience fallback: accepts an anonymous object or IDictionary. Uses reflection; avoid on hot paths.
        /// </summary>
        public IDisposable WithProperties(object state)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
                return _logger.BeginScope(kvps);

            var dict = new Dictionary<string, object?>();
            var props = state.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];
                dict[p.Name] = p.GetValue(state);
            }
            return _logger.BeginScope(dict);
        }

        // ------ Shorthand helpers (optional but convenient) ------
        public void Trace(string messageTemplate, params object?[] args) => Log(LogLevel.Trace, default, null, messageTemplate, args);
        public void Debug(string messageTemplate, params object?[] args) => Log(LogLevel.Debug, default, null, messageTemplate, args);
        public void Info(string messageTemplate, params object?[] args) => Log(LogLevel.Information, default, null, messageTemplate, args);
        public void Warn(string messageTemplate, params object?[] args) => Log(LogLevel.Warning, default, null, messageTemplate, args);
        public void Error(string messageTemplate, params object?[] args) => Log(LogLevel.Error, default, null, messageTemplate, args);
        public void Error(Exception ex, string messageTemplate, params object?[] args) => Log(LogLevel.Error, default, ex, messageTemplate, args);
        public void Critical(Exception ex, string messageTemplate, params object?[] args) => Log(LogLevel.Critical, default, ex, messageTemplate, args);
    }
}
