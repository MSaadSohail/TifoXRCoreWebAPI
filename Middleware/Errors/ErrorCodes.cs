namespace GMS.TifoXRCoreWebAPI.Errors
{
    public enum ErrorCodes
    {
        // 1xxx: Auth/AuthZ
        Unauthorized = 1000,
        InvalidCredentials = 1001,
        TokenExpired = 1002,
        TokenInvalid = 1003,
        AccessDenied = 1004,

        // 2xxx: Request/Validation
        BadRequest = 2000,
        MissingParameter = 2001,
        InvalidParameter = 2002,
        InvalidFormat = 2003,
        ValidationFailed = 2004,
        PayloadTooLarge = 2005,
        UnsupportedMediaType = 2006,

        // 3xxx: Resource/Domain
        NotFound = 3000,
        UserNotFound = 3001,
        ItemNotFound = 3002,
        RouteNotFound = 3003,
        MethodNotAllowed = 3004,

        // 4xxx: Conflict/State
        Conflict = 4000,
        AlreadyExists = 4001,
        ResourceLocked = 4002,
        TooManyRequests = 4003,
        PreconditionFailed = 4004,
        StateNotPermitted = 4005,

        // 5xxx: System/Internal
        InternalServerError = 5000,
        DatabaseError = 5001,
        ServiceUnavailable = 5002,
        Timeout = 5003,
        NotImplemented = 5004,
        DependencyFailure = 5005
    }
}
