// <copyright file="ErrorMessages.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/04/2025</date>
// <summary>Class to handle error messages</summary>

namespace GMS.TifoXRCoreWebAPI.Middleware.Errors
{
    /// Strongly-typed, parameterizable descriptor.
    public readonly record struct ErrorMessage(
        ErrorCodes Code,
        string Template,
        LogLevel DefaultLevel,
        string Category)
    {
        public string Format(params object[] args) =>
            string.Format(Template, args ?? Array.Empty<object>());
    }
}