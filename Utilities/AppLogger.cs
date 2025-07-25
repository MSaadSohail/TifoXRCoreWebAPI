using Serilog;
using RollingInterval = Serilog.RollingInterval;
namespace TifoXRCoreWebAPI.Utilities
{
    public static class AppLogger
    {
        public static void Initialize()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/TifoXRCoreWebAPI_log.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }


        public static void Info(string message) => Log.Information(message);
        public static void Warn(string message) => Log.Warning(message);
        public static void Error(Exception ex, string message) => Log.Error(ex, message);
        public static void Debug(string message) => Log.Debug(message);
        public static void Close() => Log.CloseAndFlush();
    }
}
