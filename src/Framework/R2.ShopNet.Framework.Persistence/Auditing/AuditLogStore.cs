using R2.ShopNet.Framework.Persistence.Repositories;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Framework.Persistence.Auditing;

/// <summary>
/// Default implementation of IAuditLogStore using the repository pattern.
/// </summary>
public class AuditLogStore : IAuditLogStore
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<AuditLog> _repository;

    public AuditLogStore(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _repository = _unitOfWork.Repository<AuditLog>();
    }

    public async Task<AuditLog> LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        if (auditLog == null)
        {
            throw new ArgumentNullException(nameof(auditLog));
        }

        var result = await _repository.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetByUserAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }

        var items = await _repository.FindAsync(
            a => a.UserId == userId,
            cancellationToken);

        var totalCount = items.Count;
        var pagedItems = items
            .OrderByDescending(a => a.ExecutedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedItems, totalCount);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetByEntityAsync(
        Guid entityId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Entity ID cannot be empty.", nameof(entityId));
        }

        var items = await _repository.FindAsync(
            a => a.AffectedEntityId == entityId,
            cancellationToken);

        var totalCount = items.Count;
        var pagedItems = items
            .OrderByDescending(a => a.ExecutedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedItems, totalCount);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetByCommandTypeAsync(
        string commandType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandType))
        {
            throw new ArgumentException("Command type cannot be null or empty.", nameof(commandType));
        }

        var items = await _repository.FindAsync(
            a => a.CommandType == commandType,
            cancellationToken);

        var totalCount = items.Count;
        var pagedItems = items
            .OrderByDescending(a => a.ExecutedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedItems, totalCount);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetFailuresAsync(
        DateTime from,
        DateTime to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException("From date cannot be greater than to date.");
        }

        var items = await _repository.FindAsync(
            a => !a.IsSuccess && a.ExecutedAt >= from && a.ExecutedAt <= to,
            cancellationToken);

        var totalCount = items.Count;
        var pagedItems = items
            .OrderByDescending(a => a.ExecutedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedItems, totalCount);
    }
}
