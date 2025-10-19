using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Framework.Persistence.Auditing;

/// <summary>
/// Entity for storing audit logs of all command executions in the system.
/// Tracks who did what, when, and the result.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// The user who executed the command (from ICurrentUserAccessor).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The name of the command that was executed.
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// The full type name of the command.
    /// </summary>
    public string CommandType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized command data (JSON).
    /// </summary>
    public string? CommandData { get; set; }

    /// <summary>
    /// Timestamp when the command was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Duration of command execution in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Whether the command succeeded or failed.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the command failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if the command failed.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// The IP address of the request (if available).
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// The user agent of the request (if available).
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// The entity ID that was affected (if applicable).
    /// </summary>
    public Guid? AffectedEntityId { get; set; }

    /// <summary>
    /// The type of entity that was affected (if applicable).
    /// </summary>
    public string? AffectedEntityType { get; set; }

    /// <summary>
    /// The operation performed (Create, Update, Delete, etc.).
    /// </summary>
    public string? Operation { get; set; }

    public AuditLog()
    {
        ExecutedAt = DateTime.UtcNow;
    }

    public static AuditLog CreateSuccess(
        string commandName,
        string commandType,
        long durationMs,
        string? userId = null,
        string? commandData = null)
    {
        return new AuditLog
        {
            UserId = userId,
            CommandName = commandName,
            CommandType = commandType,
            CommandData = commandData,
            DurationMs = durationMs,
            IsSuccess = true,
            ExecutedAt = DateTime.UtcNow
        };
    }

    public static AuditLog CreateFailure(
        string commandName,
        string commandType,
        long durationMs,
        Exception exception,
        string? userId = null,
        string? commandData = null)
    {
        return new AuditLog
        {
            UserId = userId,
            CommandName = commandName,
            CommandType = commandType,
            CommandData = commandData,
            DurationMs = durationMs,
            IsSuccess = false,
            ErrorMessage = exception.Message,
            StackTrace = exception.StackTrace,
            ExecutedAt = DateTime.UtcNow
        };
    }

    public void SetAffectedEntity(Guid entityId, string entityType, string operation)
    {
        AffectedEntityId = entityId;
        AffectedEntityType = entityType;
        Operation = operation;
    }

    public void SetRequestInfo(string? ipAddress, string? userAgent)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
