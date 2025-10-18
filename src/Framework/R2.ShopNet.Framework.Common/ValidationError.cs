namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Represents a validation error with property name and error messages.
/// </summary>
public sealed record ValidationError
{
    public string PropertyName { get; }
    public string[] ErrorMessages { get; }

    public ValidationError(string propertyName, params string[] errorMessages)
    {
        PropertyName = propertyName;
        ErrorMessages = errorMessages;
    }

    public static ValidationError Create(string propertyName, params string[] errorMessages) =>
        new(propertyName, errorMessages);
}

/// <summary>
/// Contains factory methods for creating validation errors.
/// </summary>
public static class ValidationErrors
{
    public static Error Required(string propertyName) =>
        Error.Validation(
            $"Validation.{propertyName}.Required",
            $"{propertyName} is required");

    public static Error Invalid(string propertyName) =>
        Error.Validation(
            $"Validation.{propertyName}.Invalid",
            $"{propertyName} is invalid");

    public static Error TooLong(string propertyName, int maxLength) =>
        Error.Validation(
            $"Validation.{propertyName}.TooLong",
            $"{propertyName} must not exceed {maxLength} characters");

    public static Error TooShort(string propertyName, int minLength) =>
        Error.Validation(
            $"Validation.{propertyName}.TooShort",
            $"{propertyName} must be at least {minLength} characters");

    public static Error OutOfRange(string propertyName, object min, object max) =>
        Error.Validation(
            $"Validation.{propertyName}.OutOfRange",
            $"{propertyName} must be between {min} and {max}");
}
