
namespace GMS.TifoXRCoreWebAPI.Errors
{
    public static class ErrorMessages
    {
        public static readonly Dictionary<int, string> Messages = new()
        {
            [1000] = "Authentication required.",
            [1001] = "Invalid username or password.",
            [1002] = "Authentication token has expired.",
            [1003] = "Invalid authentication token.",
            [1004] = "You do not have permission to access this resource.",

            [2000] = "The request was invalid or cannot be served.",
            [2001] = "Required parameter is missing.",
            [2002] = "Invalid value for parameter.",
            [2003] = "Invalid data format.",
            [2004] = "Validation failed for the request.",
            [2005] = "The request payload is too large.",
            [2006] = "Unsupported content type.",

            [3000] = "The requested resource could not be found.",
            [3001] = "User not found.",
            [3002] = "Item not found.",
            [3003] = "Endpoint does not exist.",
            [3004] = "HTTP method not allowed.",

            [4000] = "Resource conflict occurred.",
            [4001] = "Resource already exists.",
            [4002] = "Resource is locked and cannot be modified.",
            [4003] = "Too many requests. Please try again later.",
            [4004] = "Precondition failed.",
            [4005] = "Operation not permitted in current resource state.",

            [5000] = "An internal server error occurred.",
            [5001] = "A database error occurred.",
            [5002] = "The service is temporarily unavailable.",
            [5003] = "The request timed out.",
            [5004] = "This functionality is not yet implemented.",
            [5005] = "External dependency/service failed."
        };
    }
}
