namespace R2.ShopNet.Framework.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public IReadOnlyList<ValidationFailure> Errors { get; }

    private ValidationResult(IEnumerable<ValidationFailure> errors)
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public static ValidationResult Success() => new(Enumerable.Empty<ValidationFailure>());

    public static ValidationResult Failure(params ValidationFailure[] errors) => new(errors);

    public static ValidationResult Failure(IEnumerable<ValidationFailure> errors) => new(errors);
}
