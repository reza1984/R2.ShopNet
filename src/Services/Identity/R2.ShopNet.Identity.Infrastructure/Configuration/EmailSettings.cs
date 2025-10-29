namespace R2.ShopNet.Identity.Infrastructure.Configuration;

/// <summary>
/// Email configuration settings.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int Port { get; set; } = 1025;

    /// <summary>
    /// Enable SSL/TLS.
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    /// SMTP username (optional).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// SMTP password (optional).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// From email address.
    /// </summary>
    public string FromEmail { get; set; } = "noreply@shopnet.com";

    /// <summary>
    /// From display name.
    /// </summary>
    public string FromName { get; set; } = "ShopNet";

    /// <summary>
    /// Client application base URL for generating links.
    /// </summary>
    public string ClientBaseUrl { get; set; } = "http://localhost:4200";
}
