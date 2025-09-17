// AppLoggerExtensions.cs
using GMS.TifoXRCoreWebAPI.Utilities.Logger.Interface; // IAppLogger<T>

namespace GMS.TifoXRCoreWebAPI.Utilities.Logger
{
    public static class AppLoggerExtensions
    {
        /// <summary>
        /// Opens a logging scope with structured exception context properties.
        /// Use in a using(...) block to attach properties to all logs inside.
        /// </summary>
        public static IDisposable WithExceptionContext<T>(
            this IAppLogger<T> log,
            string issue,
            string methodName,
            object? parameters = null,
            string? extra = null)
        {
            // Turn anonymous object or dictionary into a scope (IAppLogger<T>.WithProperties handles either)
            var bag = new Dictionary<string, object?>
            {
                ["Issue"] = issue,
                ["Method"] = methodName
            };

            if (parameters is not null) bag["Params"] = parameters; // stays structured, not serialized text
            if (!string.IsNullOrWhiteSpace(extra)) bag["Details"] = extra;

            return log.WithProperties(bag);
        }

        /// <summary>
        /// Convenience helper: logs an exception with a scope carrying Issue/Method/Params/Details.
        /// </summary>
        public static void ErrorWithContext<T>(
            this IAppLogger<T> log,
            Exception ex,
            string messageTemplate,
            string issue,
            string methodName,
            object? parameters = null,
            string? extra = null,
            params object?[] args)
        {
            using (log.WithExceptionContext(issue, methodName, parameters, extra))
            {
                log.Error(ex, messageTemplate, args);
            }
        }
    }
}
