using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Framework.Persistence.Auditing;

/// <summary>
/// Decorator that wraps command handlers to automatically log command executions.
/// Captures timing, success/failure, and stores audit logs after successful persistence.
/// </summary>
public class AuditingCommandHandlerDecorator<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly ICommandHandler<TCommand, TResponse> _inner;
    private readonly IAuditLogStore _auditLogStore;
    private readonly ICurrentUserAccessor? _currentUserAccessor;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AuditingCommandHandlerDecorator(
        ICommandHandler<TCommand, TResponse> inner,
        IAuditLogStore auditLogStore,
        ICurrentUserAccessor? currentUserAccessor = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _auditLogStore = auditLogStore ?? throw new ArgumentNullException(nameof(auditLogStore));
        _currentUserAccessor = currentUserAccessor;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var commandType = command.GetType();
        var commandName = commandType.Name;
        var commandTypeFullName = commandType.FullName ?? commandType.Name;

        string? commandData = null;
        try
        {
            commandData = JsonSerializer.Serialize(command, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch
        {
            // If serialization fails, just skip the command data
            commandData = null;
        }

        var userId = _currentUserAccessor?.GetCurrentUserId();
        AuditLog auditLog;

        try
        {
            // Execute the actual command handler
            var result = await _inner.Handle(command, cancellationToken);
            stopwatch.Stop();

            // Create success audit log
            auditLog = AuditLog.CreateSuccess(
                commandName,
                commandTypeFullName,
                stopwatch.ElapsedMilliseconds,
                userId,
                commandData);

            // Try to extract affected entity info from the result if it's a Result type
            TrySetAffectedEntityFromResult(auditLog, result);

            // Set request info if available
            SetRequestInfo(auditLog);

            // Persist the audit log (this happens AFTER the command succeeds)
            await _auditLogStore.LogAsync(auditLog, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Create failure audit log
            auditLog = AuditLog.CreateFailure(
                commandName,
                commandTypeFullName,
                stopwatch.ElapsedMilliseconds,
                ex,
                userId,
                commandData);

            // Set request info if available
            SetRequestInfo(auditLog);

            // Persist the failure audit log
            try
            {
                await _auditLogStore.LogAsync(auditLog, cancellationToken);
            }
            catch
            {
                // Don't fail the original exception if audit logging fails
                // In production, you might want to log this to a separate error tracking system
            }

            // Re-throw the original exception
            throw;
        }
    }

    private void TrySetAffectedEntityFromResult(AuditLog auditLog, TResponse result)
    {
        // Try to extract entity information from Result<T> types
        if (result == null) return;

        var resultType = result.GetType();

        // Check if it's a Result type with a Value property
        if (resultType.IsGenericType)
        {
            var genericTypeDef = resultType.GetGenericTypeDefinition();
            if (genericTypeDef == typeof(Result<>))
            {
                var valueProperty = resultType.GetProperty("Value");
                if (valueProperty != null)
                {
                    var value = valueProperty.GetValue(result);
                    if (value != null)
                    {
                        TryExtractEntityInfo(auditLog, value);
                    }
                }
            }
        }
    }

    private void TryExtractEntityInfo(AuditLog auditLog, object entity)
    {
        var entityType = entity.GetType();

        // Try to get Id property
        var idProperty = entityType.GetProperty("Id");
        if (idProperty != null && idProperty.PropertyType == typeof(Guid))
        {
            var id = (Guid?)idProperty.GetValue(entity);
            if (id.HasValue && id.Value != Guid.Empty)
            {
                // Infer operation based on command name
                var operation = InferOperation();
                auditLog.SetAffectedEntity(id.Value, entityType.Name, operation);
            }
        }
    }

    private string InferOperation()
    {
        var commandName = typeof(TCommand).Name.ToLowerInvariant();

        if (commandName.Contains("create") || commandName.Contains("add") || commandName.Contains("register"))
            return "Create";
        if (commandName.Contains("update") || commandName.Contains("edit") || commandName.Contains("modify"))
            return "Update";
        if (commandName.Contains("delete") || commandName.Contains("remove"))
            return "Delete";
        if (commandName.Contains("activate"))
            return "Activate";
        if (commandName.Contains("deactivate"))
            return "Deactivate";

        return "Unknown";
    }

    private void SetRequestInfo(AuditLog auditLog)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null) return;

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

        auditLog.SetRequestInfo(ipAddress, userAgent);
    }
}
