namespace R2.ShopNet.Framework.Persistence.Auditing;

/// <summary>
/// Interface for storing audit logs of command executions.
/// </summary>
public interface IAuditLogStore
{
    /// <summary>
    /// Persists an audit log entry to the database.
    /// </summary>
    /// <param name="auditLog">The audit log entry to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted audit log with generated ID.</returns>
    Task<AuditLog> LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to filter by.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of audit logs.</returns>
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetByUserAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs for a specific entity.
    /// </summary>
    /// <param name="entityId">The entity ID to filter by.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of audit logs.</returns>
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetByEntityAsync(
        Guid entityId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs for a specific command type.
    /// </summary>
    /// <param name="commandType">The command type to filter by.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of audit logs.</returns>
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetByCommandTypeAsync(
        string commandType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves failed command executions within a time range.
    /// </summary>
    /// <param name="from">Start of time range.</param>
    /// <param name="to">End of time range.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of failed audit logs.</returns>
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetFailuresAsync(
        DateTime from,
        DateTime to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
