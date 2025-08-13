// <copyright file="GlobalException.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>08/04/2025</date>
// <summary>Middleware Class to global exception handling</summary>

using System.Data;
using System.Net;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

using GMS.TifoXRCoreWebAPI.Errors;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;

namespace GMS.TifoXRCoreWebAPI.Middleware
{
    public class GlobalException(RequestDelegate next, ILogger<GlobalException> logger, IHostEnvironment env)
    {

        const string ERROR_MESSAGE = "NULL REFERENECE for {0} in Function {1}";

        private readonly RequestDelegate _next = next;
        private readonly ILogger<GlobalException> _logger = logger;
        private readonly IHostEnvironment _env = env;

        private static JsonSerializerOptions JsonOptions { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Middleware entry point. Invoked automatically by the ASP.NET Core pipeline for every HTTP request.
        /// Executes the next middleware/component, and catches any unhandled exceptions.
        /// If an exception occurs, handles it via HandleExceptionAsync and returns a standardized error response.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        ///<summary>
        /// Middleware entry point for handling HTTP requests.
        /// Executes the next middleware in the pipeline and catches any unhandled exceptions.
        /// If an exception occurs, it delegates error handling to <see cref="HandleExceptionAsync"/> 
        /// to return a standardized error response.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (code, status, message) = GetErrorInfo(exception);
            var traceId = context.TraceIdentifier;
            string requestBody = await ReadRequestBodyAsync(context);

            _logger.LogError(
                exception,
                "Exception caught in GlobalExceptionMiddleware. Path: {Path}, Query: {QueryString}, Body: {Body}",
                context.Request.Path,
                context.Request.QueryString,
                requestBody
            );

            var response = new ErrorResponse
            {
                Code = code,
                ErrorMessage = message,
                TraceId = traceId,
                Details = _env.IsDevelopment() ? exception.Message : null
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = status;

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }

        /// <summary>
        /// Maps an exception to a standardized error code, HTTP status code, and user-friendly error message.
        /// Inspects the exception type and returns a tuple containing the appropriate error code, HTTP status, and message
        /// for consistent error responses throughout the API.
        /// </summary>
        private static (int code, int status, string message) GetErrorInfo(Exception exception)
        {
            int code = (int)ErrorCodes.InternalServerError;
            int status = (int)HttpStatusCode.InternalServerError;
            string message;

            switch (exception)
            {
                case ArgumentNullException:
                    code = (int)ErrorCodes.MissingParameter;
                    status = StatusCodes.Status400BadRequest;
                    break;
                case ArgumentException:
                    code = (int)ErrorCodes.InvalidParameter;
                    status = StatusCodes.Status400BadRequest;
                    break;
                case FormatException:
                    code = (int)ErrorCodes.InvalidFormat;
                    status = StatusCodes.Status400BadRequest;
                    break;
                case ValidationException:
                    code = (int)ErrorCodes.ValidationFailed;
                    status = StatusCodes.Status422UnprocessableEntity;
                    break;
                case KeyNotFoundException:
                    code = (int)ErrorCodes.NotFound;
                    status = StatusCodes.Status404NotFound;
                    break;
                case NotSupportedException:
                    code = (int)ErrorCodes.MethodNotAllowed;
                    status = StatusCodes.Status405MethodNotAllowed;
                    break;
                case UnauthorizedAccessException:
                    code = (int)ErrorCodes.AccessDenied;
                    status = StatusCodes.Status403Forbidden;
                    break;
                case InvalidOperationException:
                    code = (int)ErrorCodes.StateNotPermitted;
                    status = StatusCodes.Status409Conflict;
                    break;
                case TimeoutException:
                    code = (int)ErrorCodes.Timeout;
                    status = StatusCodes.Status504GatewayTimeout;
                    break;
                case DBConcurrencyException:
                    code = (int)ErrorCodes.Conflict;
                    status = StatusCodes.Status409Conflict;
                    break;
                case DataException:
                    code = (int)ErrorCodes.DatabaseError;
                    status = StatusCodes.Status500InternalServerError;
                    break;
                case NotImplementedException:
                    code = (int)ErrorCodes.NotImplemented;
                    status = StatusCodes.Status501NotImplemented;
                    break;
                case HttpRequestException:
                    code = (int)ErrorCodes.DependencyFailure;
                    status = StatusCodes.Status502BadGateway;
                    break;
                case ResourceNotFoundException:
                    code = (int)ErrorCodes.NotFound;
                    status = StatusCodes.Status404NotFound;
                    break;
                case ConflictException:
                    code = (int)ErrorCodes.Conflict;
                    status = StatusCodes.Status409Conflict;
                    break;
            }

            message = ErrorMessages.Messages.TryGetValue(code, out var msg) 
                ? msg 
                : ErrorMessages.Messages[(int)ErrorCodes.InternalServerError];
            
            return (code, status, message);
        }

        /// <summary>
        /// Reads the request body as a string for logging purposes.
        /// Resets the stream position before and after reading to avoid interfering with downstream middleware.
        /// Returns an empty string if the request body is empty or not seekable.
        /// </summary>
        private static async Task<string> ReadRequestBodyAsync(HttpContext context)
        {
            if (context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                string body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
                return body;
            }
            return string.Empty;
        }

        /// <summary>
        /// Formats an exception message for logging or debugging purposes.
        /// Includes the method name, issue description, optional parameters, and extra details.
        /// <param name="issue">A short description of the issue or exception.</param>
        /// <param name="methodName">The name of the method where the exception occurred.</param>
        /// <param name="parameters">Optional parameters related to the exception context (default: null).</param>
        /// <param name="extra">Optional extra details to include in the message (default: null).</param>
        /// </summary>
        public static string FormatExceptionMessage(
            string issue,
            string methodName,
            object? parameters = null,
            string? extra = null)
        {
            var paramStr = parameters == null 
                ? "" 
                : $" | Params: {JsonSerializer.Serialize(parameters)}";

            var extraStr = string.IsNullOrEmpty(extra) 
                ? "" 
                : $" | Details: {extra}";

            return $"Method: [{methodName}]{paramStr}{extraStr} | Issue: {issue}";
        }

        public static string FormatExceptionMessage(
            string errorMesage, params object[] args)
        {
            return string.Format(errorMesage, args);
        }
    }

    public class ErrorResponse
    {
        public int Code { get; set; }
        public string ErrorMessage { get; set; }
        public string TraceId { get; set; }
        public string? Details { get; set; }
    }
}
