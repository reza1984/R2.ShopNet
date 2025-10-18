namespace R2.ShopNet.Framework.Validation;

/// <summary>
/// Represents a validation failure.
/// </summary>
public sealed record ValidationFailure
{
    public string PropertyName { get; }
    public string ErrorMessage { get; }
    public string ErrorCode { get; }

    public ValidationFailure(string propertyName, string errorMessage, string errorCode = "")
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }
}
