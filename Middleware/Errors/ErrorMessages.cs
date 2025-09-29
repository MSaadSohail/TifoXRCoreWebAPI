// <copyright file="ErrorMessages.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/05/2025</date>
// <summary>Class to handle error messages</summary>

namespace GMS.TifoXRCoreWebAPI.Middleware.Errors
{
    public static class ErrorMessages
    {
        public static class Validation
        {
            public static readonly ErrorMessage BadRequest =
                new(ErrorCodes.BadRequest, "The request was invalid or cannot be served.", LogLevel.Warning, "HTTP");

            public static readonly ErrorMessage PositiveIntRequired =
                new(ErrorCodes.InvalidParameter, "{0} must be a positive integer.", LogLevel.Warning, "Validation");

            public static readonly ErrorMessage MissingParameter =
                new(ErrorCodes.MissingParameter, "{0} cannot be null.", LogLevel.Warning, "Validation");

            public static readonly ErrorMessage InvalidFormat =
                new(ErrorCodes.InvalidFormat, "Invalid format for {0}.", LogLevel.Warning, "Validation");

            public static readonly ErrorMessage EmptyCollection =
                new(ErrorCodes.InvalidParameter, "{0} cannot be empty.", LogLevel.Warning, "Validation");

            public static readonly ErrorMessage MustHaveValidIds =
                new(ErrorCodes.InvalidParameter, "Each {0} must have a valid (positive) Id.", LogLevel.Warning, "Validation");

            public static readonly ErrorMessage ValidationFailed =
                new(ErrorCodes.ValidationFailed, "Validation failed for the request.", LogLevel.Warning, "Validation");

            public static readonly ErrorMessage RouteBodyMismatch =
                new(ErrorCodes.InvalidParameter, "Route value for {0} must match the id in the body.", LogLevel.Warning, "Validation");
        }

        // -------- Auth (401/403) --------
        public static class Auth
        {
            public static readonly ErrorMessage Unauthorized =
                new(ErrorCodes.Unauthorized, "Authentication required or credentials invalid.", LogLevel.Warning, "Auth");

            public static readonly ErrorMessage AccessDenied =
                new(ErrorCodes.AccessDenied, "You do not have permission to access this resource.", LogLevel.Warning, "Auth");
        }

        // -------- HTTP / General (4xx/5xx generic) --------
        public static class Http
        {
            public static readonly ErrorMessage BadRequest =
                new(ErrorCodes.BadRequest, "The request was invalid or cannot be served.", LogLevel.Warning, "HTTP");

            public static readonly ErrorMessage NotFound =
                new(ErrorCodes.NotFound, "The requested resource could not be found.", LogLevel.Information, "HTTP");

            public static readonly ErrorMessage MethodNotAllowed =
                new(ErrorCodes.MethodNotAllowed, "HTTP method not allowed for this endpoint.", LogLevel.Warning, "HTTP");

            public static readonly ErrorMessage Conflict =
                new(ErrorCodes.Conflict, "Resource conflict occurred.", LogLevel.Warning, "HTTP");

            public static readonly ErrorMessage PreconditionFailed =
                new(ErrorCodes.PreconditionFailed, "Precondition failed for the request.", LogLevel.Warning, "HTTP");

            public static readonly ErrorMessage ResourceLocked =
                new(ErrorCodes.ResourceLocked, "Resource is locked and cannot be modified.", LogLevel.Warning, "HTTP");

            public static readonly ErrorMessage TooManyRequests =
                new(ErrorCodes.TooManyRequests, "Too many requests. Please try again later.", LogLevel.Warning, "HTTP");

            public static readonly ErrorMessage PayloadTooLarge =
                new(ErrorCodes.PayloadTooLarge, "The request payload is too large.", LogLevel.Warning, "HTTP");

            public static readonly ErrorMessage UnsupportedMediaType =
                new(ErrorCodes.UnsupportedMediaType, "Unsupported content type.", LogLevel.Warning, "HTTP");

            public static readonly ErrorMessage Timeout =
                new(ErrorCodes.Timeout, "The request timed out.", LogLevel.Error, "HTTP");

            public static readonly ErrorMessage NotImplemented =
                new(ErrorCodes.NotImplemented, "This functionality is not yet implemented.", LogLevel.Warning, "HTTP");

            public static readonly ErrorMessage DependencyFailure =
                new(ErrorCodes.DependencyFailure, "External dependency/service failed.", LogLevel.Error, "HTTP");

            public static readonly ErrorMessage ServiceUnavailable =
                new(ErrorCodes.ServiceUnavailable, "The service is temporarily unavailable.", LogLevel.Error, "HTTP");

            public static readonly ErrorMessage InternalServerError =
                new(ErrorCodes.InternalServerError, "An internal server error occurred.", LogLevel.Error, "HTTP");
        }

        // -------- Data (5xx data layer) --------
        public static class Data
        {
            public static readonly ErrorMessage DatabaseError =
                new(ErrorCodes.DatabaseError, "A database error occurred.", LogLevel.Error, "Data");
        }

        // -------- Domain (409-like semantic states) --------
        public static class Domain
        {
            public static readonly ErrorMessage StateNotPermitted =
                new(ErrorCodes.StateNotPermitted, "Operation is not permitted in the current state.", LogLevel.Warning, "Domain");
        }
    }
}