using GMS.TifoXRCoreWebAPI.Utilities.Logger.Interface;

using Microsoft.Extensions.Logging;

namespace GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Fakes
{
    public sealed class NullAppLogger<T> : IAppLogger<T>
    {
        public bool IsEnabled(LogLevel level) => false;

        public void Debug(string message, params object?[] args) { }
        public void Warn(string message, params object?[] args) { }
        public void Error(Exception ex, string message, params object?[] args) { }

        // Match the interface used by BoothRepository: returns IDisposable
        public IDisposable WithProperties(params (string Key, object Value)[] properties)
            => EmptyScope.Instance;

        bool IAppLogger<T>.IsEnabled(LogLevel level)
        {
            throw new NotImplementedException();
        }

        void IAppLogger<T>.Log(LogLevel level, EventId eventId, Exception? ex, string messageTemplate, params object?[] args)
        {
            throw new NotImplementedException();
        }

        void IAppLogger<T>.Debug(string messageTemplate, params object?[] args)
        {
            throw new NotImplementedException();
        }

        void IAppLogger<T>.Warn(string messageTemplate, params object?[] args)
        {
            throw new NotImplementedException();
        }

        void IAppLogger<T>.Error(Exception ex, string messageTemplate, params object?[] args)
        {
            throw new NotImplementedException();
        }

        IDisposable IAppLogger<T>.BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        IDisposable IAppLogger<T>.WithProperties(object state)
        {
            throw new NotImplementedException();
        }

        private sealed class EmptyScope : IDisposable
        {
            public static readonly EmptyScope Instance = new();
            public void Dispose() { }
        }
    }

}

