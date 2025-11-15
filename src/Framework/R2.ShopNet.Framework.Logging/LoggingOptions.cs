namespace R2.ShopNet.Framework.Logging;

/// <summary>
/// Configuration options for R2.ShopNet logging
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Gets or sets the application name used in logs
    /// </summary>
    public string ApplicationName { get; set; } = "R2.ShopNet";

    /// <summary>
    /// Gets or sets whether to enable file logging
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the file logging path
    /// </summary>
    public string LogFilePath { get; set; } = "logs/app-.log";

    /// <summary>
    /// Gets or sets whether to enable Seq logging
    /// </summary>
    public bool EnableSeqLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the Seq server URL
    /// </summary>
    public string? SeqServerUrl { get; set; }

    /// <summary>
    /// Gets or sets the Seq API key
    /// </summary>
    public string? SeqApiKey { get; set; }

    /// <summary>
    /// Gets or sets the minimum log level
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets whether to enrich logs with machine name
    /// </summary>
    public bool EnrichWithMachineName { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enrich logs with environment name
    /// </summary>
    public bool EnrichWithEnvironmentName { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enrich logs with thread ID
    /// </summary>
    public bool EnrichWithThreadId { get; set; } = true;
}
