namespace R2.ShopNet.Framework.Validation;

/// <summary>
/// Interface for validators.
/// </summary>
public interface IValidator<in T>
{
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}
