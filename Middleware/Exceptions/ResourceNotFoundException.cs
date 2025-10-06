
using System.Runtime.Serialization;

namespace GMS.TifoXRCoreWebAPI.Middleware.Exceptions
{
    [Serializable]
    public class ResourceNotFoundException : SystemException
    {
        public ResourceNotFoundException()
            : base("The requested resource could not be found.") { }

        public ResourceNotFoundException(string? message)
            : base(message) { }

        public ResourceNotFoundException(string? message, Exception? innerException)
            : base(message, innerException) { }

        protected ResourceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
