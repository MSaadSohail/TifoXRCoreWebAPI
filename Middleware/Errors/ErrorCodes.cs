// <copyright file="ErrorCodes.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>08/04/2025</date>
// <summary>Enum of error codes</summary>

namespace TifoXRCoreWebAPI.Errors
{
    public enum ErrorCodes
    {
        // 400: Bad Request and validation
        BadRequest = 400,
        MissingParameter = 400,
        InvalidParameter = 400,
        InvalidFormat = 400,
        ValidationFailed = 422,        // 422: Unprocessable Entity

        PayloadTooLarge = 413,
        UnsupportedMediaType = 415,

        // 401: Unauthorized
        Unauthorized = 401,
        InvalidCredentials = 401,
        TokenExpired = 401,
        TokenInvalid = 401,

        // 403: Forbidden
        AccessDenied = 403,

        // 404: Not Found
        NotFound = 404,
        UserNotFound = 404,
        ItemNotFound = 404,
        RouteNotFound = 404,

        // 405: Method Not Allowed
        MethodNotAllowed = 405,

        // 409: Conflict
        Conflict = 409,
        AlreadyExists = 409,
        ResourceLocked = 423,          // 423: Locked
        TooManyRequests = 429,         // 429: Too Many Requests
        PreconditionFailed = 412,      // 412: Precondition Failed
        StateNotPermitted = 409,

        // 500: Server errors
        InternalServerError = 500,
        DatabaseError = 500,
        ServiceUnavailable = 503,
        Timeout = 504,                 // 504: Gateway Timeout
        NotImplemented = 501,          // 501: Not Implemented
        DependencyFailure = 502        // 502: Bad Gateway
    }
}
