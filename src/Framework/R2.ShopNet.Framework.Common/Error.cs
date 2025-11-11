namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Represents an error with a code, message, and type.
/// </summary>
public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided", ErrorType.Validation);

    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }

    private Error(string code, string message, ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);
}

/// <summary>
/// Specifies the type of error, aligned with HTTP status codes.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// No error occurred (200 OK)
    /// </summary>
    None = 0,
    
    /// <summary>
    /// General failure (500 Internal Server Error)
    /// </summary>
    Failure = 500,
    
    /// <summary>
    /// Validation error (400 Bad Request)
    /// </summary>
    Validation = 400,
    
    /// <summary>
    /// Resource not found (404 Not Found)
    /// </summary>
    NotFound = 404,
    
    /// <summary>
    /// Conflict with current state (409 Conflict)
    /// </summary>
    Conflict = 409,
    
    /// <summary>
    /// Authentication required (401 Unauthorized)
    /// </summary>
    Unauthorized = 401,
    
    /// <summary>
    /// Access forbidden (403 Forbidden)
    /// </summary>
    Forbidden = 403
}
