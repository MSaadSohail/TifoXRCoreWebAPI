// <copyright file="ErrorService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/05/2025</date>
// <summary>Unified logging/exception service</summary>

using System.Data;
using System.Reflection;
using System.Text.Json;

namespace GMS.TifoXRCoreWebAPI.Middleware.Errors
{
    public enum ErrorType
    {
        Argument,
        ArgumentNull,
        InvalidOperation,
        NotFound
    }

    public static class ErrorService
    {
        public static Exception Log(
            ErrorType type,
            string method,
            ErrorMessage message,
            string? paramName = null,
            object? parameters = null,
            string? extra = null,
            ILogger? logger = null)
        {
            // Fill placeholders (e.g., "{0}" => param name)
            var text = !string.IsNullOrWhiteSpace(paramName) ? message.Format(paramName!) : message.Template;

            // Make 404s specific by reflecting parameters
            if (type == ErrorType.NotFound)
                text = BuildNotFoundIssue(parameters);

            // Final body
            var body = Format(method, text, parameters, extra);

            // Optional structured log (for simple logging scenarios)
            logger?.Log(
                message.DefaultLevel,
                "API error: {Category} | Code={Code} | Method={Method} | Issue={Issue} | Params={Params}",
                message.Category,
                (int)message.Code,
                method,
                text,
                parameters
            );

            return type switch
            {
                ErrorType.Argument => new ArgumentException(body, paramName),
                ErrorType.ArgumentNull => new ArgumentNullException(paramName, body),
                ErrorType.InvalidOperation => new InvalidOperationException(body),
                ErrorType.NotFound => new Middleware.Exceptions.ResourceNotFoundException(body),
                _ => new Exception(body)
            };
        }

        //CAN BE USED IN FUTURE - KEPT FOR NOW

        //public static ArgumentException Arg(string method, ErrorMessage msg, string paramName, object? parameters = null, string? extra = null)
        //    => new ArgumentException(Format(method, msg.Template, parameters, extra), paramName);

        //public static ArgumentNullException ArgNull(string method, ErrorMessage msg, string paramName, object? parameters = null, string? extra = null)
        //    => new ArgumentNullException(paramName, Format(method, msg.Template, parameters, extra));

        //public static InvalidOperationException InvalidOp(string method, ErrorMessage msg, object? parameters = null, string? extra = null)
        //    => new InvalidOperationException(Format(method, msg.Template, parameters, extra));

        //public static Exception NotFound(string method, object? parameters = null, string? extra = null)
        //    => new Middleware.Exceptions.ResourceNotFoundException(
        //        Format(method, ErrorMessages.Http.NotFound.Template, parameters, extra));

        public static ErrorMessage Resolve(ErrorCodes code) => code switch
        {
            // 4xx
            ErrorCodes.BadRequest or
            ErrorCodes.InvalidParameter or
            ErrorCodes.MissingParameter or
            ErrorCodes.InvalidFormat => ErrorMessages.Http.BadRequest,

            ErrorCodes.ValidationFailed => ErrorMessages.Validation.ValidationFailed,
            ErrorCodes.PayloadTooLarge => ErrorMessages.Http.PayloadTooLarge,
            ErrorCodes.UnsupportedMediaType => ErrorMessages.Http.UnsupportedMediaType,

            ErrorCodes.Unauthorized => ErrorMessages.Auth.Unauthorized,
            ErrorCodes.AccessDenied => ErrorMessages.Auth.AccessDenied,

            ErrorCodes.NotFound => ErrorMessages.Http.NotFound,
            ErrorCodes.MethodNotAllowed => ErrorMessages.Http.MethodNotAllowed,

            ErrorCodes.Conflict or
            ErrorCodes.StateNotPermitted => ErrorMessages.Http.Conflict,

            ErrorCodes.PreconditionFailed => ErrorMessages.Http.PreconditionFailed,
            ErrorCodes.ResourceLocked => ErrorMessages.Http.ResourceLocked,
            ErrorCodes.TooManyRequests => ErrorMessages.Http.TooManyRequests,

            // 5xx
            ErrorCodes.InternalServerError or
            ErrorCodes.DatabaseError => ErrorMessages.Http.InternalServerError,

            ErrorCodes.ServiceUnavailable => ErrorMessages.Http.ServiceUnavailable,
            ErrorCodes.Timeout => ErrorMessages.Http.Timeout,
            ErrorCodes.NotImplemented => ErrorMessages.Http.NotImplemented,
            ErrorCodes.DependencyFailure => ErrorMessages.Http.DependencyFailure,

            _ => ErrorMessages.Http.InternalServerError
        };

        private static string Format(string methodName, string issue, object? parameters = null, string? extra = null)
        {
            var paramStr = parameters == null ? "" : $" | Params: {JsonSerializer.Serialize(parameters)}";
            var extraStr = string.IsNullOrEmpty(extra) ? "" : $" | Details: {extra}";
            return $"Method: [{methodName}]{paramStr}{extraStr} | Issue: {issue}";
        }

        private static string BuildNotFoundIssue(object? parameters)
        {
            if (parameters is null) return "The requested resource could not be found.";

            var props = parameters.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (props.Length == 0) return "The requested resource could not be found.";

            var parts = new List<string>();
            foreach (var p in props)
            {
                var val = p.GetValue(parameters, null);
                parts.Add($"{p.Name}: {val}");
            }

            return $"No data found for {string.Join(", ", parts)}.";
        }
    }
}
