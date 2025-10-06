// <copyright file="GlobalException.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/19/2025</date>
// <summary>Middleware Class for global exception handling with structured logging</summary>

using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
//
using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using GMS.TifoXRCoreWebAPI.Utilities.Logger.Interface;

namespace GMS.TifoXRCoreWebAPI.Middleware
{
    // Conventional middleware (primary constructor). Do NOT implement IMiddleware.
    public class GlobalException(RequestDelegate next, IAppLogger<GlobalException> logger, IHostEnvironment env)
    {
        private readonly RequestDelegate _next = next;
        private readonly IAppLogger<GlobalException> _logger = logger;
        private readonly IHostEnvironment _env = env;

        private static JsonSerializerOptions JsonOptions { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task Invoke(HttpContext context)
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

        // --- Core exception handling (structured + safe) ---
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (code, status, message) = GetErrorInfo(exception);
            var traceId = context.TraceIdentifier;

            // Always log one structured error event
            using (_logger.WithProperties(new
            {
                Outcome = "Failed",
                StatusCode = status,
                ErrorCode = code,
                CorrelationId = traceId,
                RequestMethod = context.Request?.Method,
                RequestPath = context.Request?.Path.Value,
                UserAgent = SafeUserAgent(context),
                QueryStringLength = context.Request?.QueryString.Value?.Length ?? 0
            }))
            {
                _logger.Error(
                    exception,
                    "Unhandled exception processing {RequestMethod} {RequestPath}",
                    context.Request?.Method,
                    context.Request?.Path.Value
                );

                // Optional debug-only previews (safe/redacted/capped)
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var (bodyPreview, bodyLen) = await ReadRequestBodyPreviewAsync(context);
                    if (bodyLen > 0)
                        _logger.Debug("RequestBodyPreview len={Length} {Preview}", bodyLen, bodyPreview);

                    var qs = context.Request?.QueryString.Value ?? string.Empty;
                    if (!string.IsNullOrEmpty(qs))
                    {
                        var redactedQs = RedactQueryString(qs);
                        _logger.Debug("QueryStringPreview len={Length} {Preview}", qs.Length, redactedQs);
                    }
                }
            }

            // Standardized error response
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

        // --- Exception → error code/status/message mapping ---
        private static (int code, int status, string message) GetErrorInfo(Exception exception)
        {
            // Prefer structured data supplied by ErrorService.Log(...)
            if (exception.Data.Contains("ErrorCode"))
            {
                var code = (int)exception.Data["ErrorCode"];

                // Default: most codes align to their HTTP status
                var status = code;

                // Adjust where our enum uses non-HTTP codes or special mappings
                if (code == (int)ErrorCodes.ValidationFailed) status = StatusCodes.Status422UnprocessableEntity; // 422
                if (code == (int)ErrorCodes.ResourceLocked) status = StatusCodes.Status423Locked;             // 423
                if (code == (int)ErrorCodes.Timeout) status = StatusCodes.Status504GatewayTimeout;    // 504
                if (code == (int)ErrorCodes.DatabaseError) status = StatusCodes.Status500InternalServerError;// 500
                if (code == (int)ErrorCodes.StateNotPermitted) status = StatusCodes.Status409Conflict;          // 409
                if (code == (int)ErrorCodes.DependencyFailure) status = StatusCodes.Status502BadGateway;        // 502
                if (code == (int)ErrorCodes.ServiceUnavailable) status = StatusCodes.Status503ServiceUnavailable;// 503

                var msg = exception.Data.Contains("UserMessage")
                    ? exception.Data["UserMessage"]?.ToString() ?? ErrorService.Resolve((ErrorCodes)code).Template
                    : ErrorService.Resolve((ErrorCodes)code).Template;

                return (code, status, msg);
            }

            // Fallback to existing type-based mapping
            int codeFallback = (int)ErrorCodes.InternalServerError;
            int statusFallback = StatusCodes.Status500InternalServerError;

            switch (exception)
            {
                case ArgumentNullException:
                    codeFallback = (int)ErrorCodes.MissingParameter; statusFallback = StatusCodes.Status400BadRequest; break;
                case ArgumentException:
                    codeFallback = (int)ErrorCodes.InvalidParameter; statusFallback = StatusCodes.Status400BadRequest; break;
                case FormatException:
                    codeFallback = (int)ErrorCodes.InvalidFormat; statusFallback = StatusCodes.Status400BadRequest; break;
                case ValidationException:
                    codeFallback = (int)ErrorCodes.ValidationFailed; statusFallback = StatusCodes.Status422UnprocessableEntity; break;
                case KeyNotFoundException:
                    codeFallback = (int)ErrorCodes.NotFound; statusFallback = StatusCodes.Status404NotFound; break;
                case NotSupportedException:
                    codeFallback = (int)ErrorCodes.MethodNotAllowed; statusFallback = StatusCodes.Status405MethodNotAllowed; break;
                case UnauthorizedAccessException:
                    codeFallback = (int)ErrorCodes.AccessDenied; statusFallback = StatusCodes.Status403Forbidden; break;
                case InvalidOperationException:
                    codeFallback = (int)ErrorCodes.StateNotPermitted; statusFallback = StatusCodes.Status409Conflict; break;
                case TimeoutException:
                    codeFallback = (int)ErrorCodes.Timeout; statusFallback = StatusCodes.Status504GatewayTimeout; break;
                case DBConcurrencyException:
                    codeFallback = (int)ErrorCodes.Conflict; statusFallback = StatusCodes.Status409Conflict; break;
                case DataException:
                    codeFallback = (int)ErrorCodes.DatabaseError; statusFallback = StatusCodes.Status500InternalServerError; break;
                case NotImplementedException:
                    codeFallback = (int)ErrorCodes.NotImplemented; statusFallback = StatusCodes.Status501NotImplemented; break;
                case HttpRequestException:
                    codeFallback = (int)ErrorCodes.DependencyFailure; statusFallback = StatusCodes.Status502BadGateway; break;
                case ResourceNotFoundException:
                    codeFallback = (int)ErrorCodes.NotFound; statusFallback = StatusCodes.Status404NotFound; break;
                case ConflictException:
                    codeFallback = (int)ErrorCodes.Conflict; statusFallback = StatusCodes.Status409Conflict; break;
            }

            var message = ErrorService.Resolve((ErrorCodes)codeFallback).Template;
            return (codeFallback, statusFallback, message);
        }

        // --- Safe, capped, redacted request body preview (JSON only) ---
        private static async Task<(string? preview, int length)> ReadRequestBodyPreviewAsync(HttpContext context)
        {
            var req = context.Request;

            if (!(req.ContentLength > 0)) return (null, 0);

            var ct = req.ContentType ?? string.Empty;
            if (!ct.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return (null, (int)(req.ContentLength ?? 0));

            try
            {
                req.EnableBuffering();
                req.Body.Position = 0;
                using var reader = new StreamReader(req.Body, leaveOpen: true);
                string json = await reader.ReadToEndAsync();
                req.Body.Position = 0;

                var redacted = RedactJson(json);
                const int cap = 2048;
                var trimmed = redacted.Length > cap ? redacted[..cap] : redacted;

                return (trimmed, json.Length);
            }
            catch
            {
                return (null, (int)(req.ContentLength ?? 0));
            }
        }

        // --- Redactors ---
        private static string RedactJson(string s)
        {
            // naive key-based redactions; extend with your patterns as needed
            s = Regex.Replace(s, "\"password\"\\s*:\\s*\".*?\"", "\"password\":\"***\"", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "\"token\"\\s*:\\s*\".*?\"", "\"token\":\"***\"", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "\"authorization\"\\s*:\\s*\".*?\"", "\"authorization\":\"***\"", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "\"secret\"\\s*:\\s*\".*?\"", "\"secret\":\"***\"", RegexOptions.IgnoreCase);
            return s;
        }

        private static string RedactQueryString(string qs)
        {
            if (string.IsNullOrEmpty(qs)) return qs;

            // Replace values for common sensitive keys; keeps separators & keys intact.
            // Pattern captures optional separator, the key, and the value; replacement keeps sep+key and masks value.
            return Regex.Replace(
                qs,
                "([?&])?(password|token|authorization|secret)=([^&]*)",
                m =>
                {
                    var sep = m.Groups[1].Success ? m.Groups[1].Value : "";
                    var key = m.Groups[2].Value;
                    return $"{sep}{key}=***";
                },
                RegexOptions.IgnoreCase);
        }

        private static string SafeUserAgent(HttpContext ctx)
        {
            var ua = ctx.Request?.Headers.UserAgent.ToString() ?? string.Empty;
            const int cap = 256;
            return ua.Length > cap ? ua[..cap] : ua;
        }

        public static string FormatExceptionMessage(string issue, string methodName, object? parameters = null, string? extra = null)
        {
            var paramStr = parameters == null ? "" : $" | Params: {JsonSerializer.Serialize(parameters)}";
            var extraStr = string.IsNullOrEmpty(extra) ? "" : $" | Details: {extra}";
            return $"Method: [{methodName}]{paramStr}{extraStr} | Issue: {issue}";
        }

        public static string FormatExceptionMessage(string errorMessage, params object[] args)
            => string.Format(errorMessage, args);
    }

    public class ErrorResponse
    {
        public int Code { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string TraceId { get; set; } = "";
        public string? Details { get; set; }
    }
}
