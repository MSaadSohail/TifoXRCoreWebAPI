
using System.Runtime.Serialization;

namespace GMS.TifoXRCoreWebAPI.Middleware.Exceptions
{
    [Serializable]
    public class ConflictException : SystemException
    {
        public ConflictException()
            : base("A conflict occurred while processing the request.") { }

        public ConflictException(string? message)
            : base(message) { }

        public ConflictException(string? message, Exception? innerException)
            : base(message, innerException) { }

        protected ConflictException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
