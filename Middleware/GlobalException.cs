using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using GMS.TifoXRCoreWebAPI.Errors;
using TifoXRCoreWebAPI.Errors;

namespace TifoXRCoreWebAPI.Middleware
{
    public class GlobalException(RequestDelegate next, ILogger<GlobalException> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<GlobalException> _logger = logger;

        /// <summary>
        /// Middleware entry point. Invoked automatically by the ASP.NET Core pipeline for every HTTP request.
        /// Executes the next middleware/component, and catches any unhandled exceptions.
        /// If an exception occurs, handles it via HandleExceptionAsync and returns a standardized error response.
        /// Do not call this method manually; it is called by the framework when the middleware is registered (e.g., with app.UseMiddleware&lt;GlobalException&gt;()).
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, _logger, GetOptions());
            }
        }

        private static JsonSerializerOptions GetOptions()
        {
            return new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger, JsonSerializerOptions options)
        {
            int code = (int)ErrorCodes.InternalServerError;
            int status = (int)HttpStatusCode.InternalServerError;
            string message = ErrorMessages.Messages[code];

            switch (exception)
            {
                case ArgumentNullException:
                    code = (int)ErrorCodes.MissingParameter;
                    status = StatusCodes.Status400BadRequest;
                    message = ErrorMessages.Messages[code];
                    break;
                case ArgumentException:
                    code = (int)ErrorCodes.InvalidParameter;
                    status = StatusCodes.Status400BadRequest;
                    message = ErrorMessages.Messages[code];
                    break;
                case FormatException:
                    code = (int)ErrorCodes.InvalidFormat;
                    status = StatusCodes.Status400BadRequest;
                    message = ErrorMessages.Messages[code];
                    break;
                case ValidationException:
                    code = (int)ErrorCodes.ValidationFailed;
                    status = StatusCodes.Status422UnprocessableEntity;
                    message = ErrorMessages.Messages[code];
                    break;
                case KeyNotFoundException:
                    code = (int)ErrorCodes.NotFound;
                    status = StatusCodes.Status404NotFound;
                    message = ErrorMessages.Messages[code];
                    break;
                case NotSupportedException:
                    code = (int)ErrorCodes.MethodNotAllowed;
                    status = StatusCodes.Status405MethodNotAllowed;
                    message = ErrorMessages.Messages[code];
                    break;
                case UnauthorizedAccessException:
                    code = (int)ErrorCodes.AccessDenied;
                    status = StatusCodes.Status403Forbidden;
                    message = ErrorMessages.Messages[code];
                    break;
                case InvalidOperationException:
                    code = (int)ErrorCodes.StateNotPermitted;
                    status = StatusCodes.Status409Conflict;
                    message = ErrorMessages.Messages[code];
                    break;
                case TimeoutException:
                    code = (int)ErrorCodes.Timeout;
                    status = StatusCodes.Status504GatewayTimeout;
                    message = ErrorMessages.Messages[code];
                    break;
                case System.Data.DBConcurrencyException:
                    code = (int)ErrorCodes.Conflict;
                    status = StatusCodes.Status409Conflict;
                    message = ErrorMessages.Messages[code];
                    break;
                case System.Data.DataException:
                    code = (int)ErrorCodes.DatabaseError;
                    status = StatusCodes.Status500InternalServerError;
                    message = ErrorMessages.Messages[code];
                    break;
                case NotImplementedException:
                    code = (int)ErrorCodes.NotImplemented;
                    status = StatusCodes.Status501NotImplemented;
                    message = ErrorMessages.Messages[code];
                    break;
                case HttpRequestException:
                    code = (int)ErrorCodes.DependencyFailure;
                    status = StatusCodes.Status502BadGateway;
                    message = ErrorMessages.Messages[code];
                    break;
                default:
                    logger.LogError(exception, "Unhandled exception occurred.");
                    break;
            }

            var response = new { code, error = message };
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(
                response,
                options: options));
        }
    }
}
