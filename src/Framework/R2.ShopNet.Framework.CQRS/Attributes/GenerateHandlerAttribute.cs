namespace R2.ShopNet.Framework.CQRS.Attributes;

/// <summary>
/// Marks a command or query handler for automatic registration via source generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateHandlerAttribute : Attribute
{
    /// <summary>
    /// Service lifetime for the handler registration (0=Singleton, 1=Scoped, 2=Transient).
    /// </summary>
    public int Lifetime { get; set; } = 1; // Scoped
}
