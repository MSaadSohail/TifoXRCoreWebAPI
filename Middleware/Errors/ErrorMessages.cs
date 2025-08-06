// <copyright file="ErrorMessages.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>08/04/2025</date>
// <summary>Class to handle error messages</summary>

namespace GMS.TifoXRCoreWebAPI.Errors
{
    public static class ErrorMessages
    {
        //FIX ME: Fetch all codes from a file hoster on server
        //Ref: ADO 406
        public static readonly Dictionary<int, string> Messages = new()
        {
            // 400 Bad Request & validation
            [400] = "The request was invalid or cannot be served.",
            [413] = "The request payload is too large.",
            [415] = "Unsupported content type.",
            [422] = "Validation failed for the request.",

            // 401 Unauthorized
            [401] = "Authentication required or credentials invalid.",

            // 403 Forbidden
            [403] = "You do not have permission to access this resource.",

            // 404 Not Found
            [404] = "The requested resource could not be found.",

            // 405 Method Not Allowed
            [405] = "HTTP method not allowed for this endpoint.",

            // 409 Conflict
            [409] = "Resource conflict occurred.",

            // 412 Precondition Failed
            [412] = "Precondition failed for the request.",

            // 423 Locked
            [423] = "Resource is locked and cannot be modified.",

            // 429 Too Many Requests
            [429] = "Too many requests. Please try again later.",

            // 500 Internal Server Error
            [500] = "An internal server error occurred.",
            [501] = "This functionality is not yet implemented.",
            [502] = "External dependency/service failed.",
            [503] = "The service is temporarily unavailable.",
            [504] = "The request timed out.",
        };
    }
}
