using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace R2.ShopNet.Framework.Configuration.Providers;

/// <summary>
/// Consul implementation of IKeyValueStore
/// </summary>
public class ConsulKeyValueStore : IKeyValueStore
{
    private readonly IConsulClient _consulClient;
    private readonly ConsulKeyValueOptions _options;
    private readonly ILogger<ConsulKeyValueStore> _logger;
    private readonly Dictionary<string, CancellationTokenSource> _watchers;
    private readonly SemaphoreSlim _connectionSemaphore;

    public ConsulKeyValueStore(
        IConsulClient consulClient,
        IOptions<ConsulKeyValueOptions> options,
        ILogger<ConsulKeyValueStore> logger)
    {
        _consulClient = consulClient;
        _options = options.Value;
        _logger = logger;
        _watchers = new Dictionary<string, CancellationTokenSource>();
        _connectionSemaphore = new SemaphoreSlim(
            _options.MaxConcurrentConnections, 
            _options.MaxConcurrentConnections);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var result = await _consulClient.KV.Get(fullKey, cancellationToken);

            if (result.Response == null)
            {
                _logger.LogDebug("Key not found in Consul: {Key}", fullKey);
                return null;
            }

            var value = System.Text.Encoding.UTF8.GetString(result.Response.Value);
            _logger.LogDebug("Retrieved value from Consul: {Key}", fullKey);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from Consul for key: {Key}", key);
            throw;
        }
    }

    public async Task<IDictionary<string, string>> GetManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>();

        foreach (var key in keys)
        {
            var value = await GetAsync(key, cancellationToken);
            if (value != null)
            {
                result[key] = value;
            }
        }

        return result;
    }

    public async Task<IDictionary<string, string>> GetByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPrefix = GetFullKey(prefix);
            var result = await _consulClient.KV.List(fullPrefix, cancellationToken);

            if (result.Response == null || result.Response.Length == 0)
            {
                _logger.LogDebug("No keys found with prefix in Consul: {Prefix}", fullPrefix);
                return new Dictionary<string, string>();
            }

            var values = new Dictionary<string, string>();
            foreach (var kv in result.Response)
            {
                if (kv.Value != null)
                {
                    var key = RemovePrefix(kv.Key);
                    var value = System.Text.Encoding.UTF8.GetString(kv.Value);
                    values[key] = value;
                }
            }

            _logger.LogDebug("Retrieved {Count} values from Consul with prefix: {Prefix}", values.Count, fullPrefix);
            return values;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting values by prefix from Consul: {Prefix}", prefix);
            throw;
        }
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var valueBytes = System.Text.Encoding.UTF8.GetBytes(value);

            var kvPair = new KVPair(fullKey)
            {
                Value = valueBytes
            };

            var result = await _consulClient.KV.Put(kvPair, cancellationToken);

            if (!result.Response)
            {
                throw new InvalidOperationException($"Failed to set value in Consul for key: {fullKey}");
            }

            _logger.LogDebug("Set value in Consul: {Key}", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in Consul for key: {Key}", key);
            throw;
        }
    }

    public async Task SetManyAsync(IDictionary<string, string> values, CancellationToken cancellationToken = default)
    {
        foreach (var kvp in values)
        {
            await SetAsync(kvp.Key, kvp.Value, cancellationToken);
        }

        _logger.LogDebug("Set {Count} values in Consul", values.Count);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var result = await _consulClient.KV.Delete(fullKey, cancellationToken);

            if (!result.Response)
            {
                _logger.LogWarning("Failed to delete key from Consul: {Key}", fullKey);
            }
            else
            {
                _logger.LogDebug("Deleted key from Consul: {Key}", fullKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key from Consul: {Key}", key);
            throw;
        }
    }

    public async Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPrefix = GetFullKey(prefix);
            var result = await _consulClient.KV.DeleteTree(fullPrefix, cancellationToken);

            if (!result.Response)
            {
                _logger.LogWarning("Failed to delete keys with prefix from Consul: {Prefix}", fullPrefix);
            }
            else
            {
                _logger.LogDebug("Deleted keys with prefix from Consul: {Prefix}", fullPrefix);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting keys by prefix from Consul: {Prefix}", prefix);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var result = await _consulClient.KV.Get(fullKey, cancellationToken);
            return result.Response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key existence in Consul: {Key}", key);
            throw;
        }
    }

    public async Task WatchAsync(string key, Action<string?> onChanged, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);

        // Cancel existing watcher if any
        if (_watchers.TryGetValue(fullKey, out var existingCts))
        {
            existingCts.Cancel();
            _watchers.Remove(fullKey);
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _watchers[fullKey] = cts;

        _logger.LogInformation("Starting watch for Consul key: {Key}", fullKey);

        _ = Task.Run(async () =>
        {
            ulong waitIndex = 0;
            int retryCount = 0;

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Acquire semaphore to limit concurrent connections
                    await _connectionSemaphore.WaitAsync(cts.Token);
                    
                    try
                    {
                        var queryOptions = new QueryOptions
                        {
                            WaitIndex = waitIndex,
                            WaitTime = TimeSpan.FromMinutes(5)
                        };

                        var result = await _consulClient.KV.Get(fullKey, queryOptions, cts.Token);

                        // Reset retry count on successful request
                        retryCount = 0;

                        if (result.LastIndex > waitIndex)
                        {
                            waitIndex = result.LastIndex;

                            var value = result.Response != null
                                ? System.Text.Encoding.UTF8.GetString(result.Response.Value)
                                : null;

                            _logger.LogDebug("Consul key changed: {Key}", fullKey);
                            onChanged(value);
                        }
                    }
                    finally
                    {
                        // Always release semaphore
                        _connectionSemaphore.Release();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Watch cancelled for Consul key: {Key}", fullKey);
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    
                    // Check if this is a rate limiting error
                    var isRateLimitError = ex.Message?.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase) == true ||
                                          ex.Message?.Contains("rate limit", StringComparison.OrdinalIgnoreCase) == true;

                    if (retryCount >= _options.MaxRetries)
                    {
                        _logger.LogError(ex, "Max retries ({MaxRetries}) reached for Consul key: {Key}. Stopping watch.", _options.MaxRetries, fullKey);
                        break;
                    }

                    // Calculate exponential backoff with jitter
                    var delaySeconds = Math.Min(
                        _options.BaseDelaySeconds * Math.Pow(2, retryCount - 1),
                        _options.MaxDelaySeconds
                    );
                    
                    // Add jitter (±20%)
                    var jitter = Random.Shared.NextDouble() * 0.4 - 0.2; // -0.2 to +0.2
                    delaySeconds *= (1 + jitter);

                    var delay = TimeSpan.FromSeconds(delaySeconds);
                    
                    if (isRateLimitError)
                    {
                        _logger.LogWarning(ex, "Rate limit error watching Consul key: {Key}. Retry {Retry}/{MaxRetries} after {Delay}s", 
                            fullKey, retryCount, _options.MaxRetries, delay.TotalSeconds);
                    }
                    else
                    {
                        _logger.LogError(ex, "Error watching Consul key: {Key}. Retry {Retry}/{MaxRetries} after {Delay}s", 
                            fullKey, retryCount, _options.MaxRetries, delay.TotalSeconds);
                    }

                    try
                    {
                        await Task.Delay(delay, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Watch cancelled during retry delay for Consul key: {Key}", fullKey);
                        break;
                    }
                }
            }

            _watchers.Remove(fullKey);
        }, cts.Token);

        await Task.CompletedTask;
    }

    private string GetFullKey(string key)
    {
        if (string.IsNullOrEmpty(_options.KeyPrefix))
        {
            return key;
        }

        return $"{_options.KeyPrefix.TrimEnd('/')}/{key.TrimStart('/')}";
    }

    private string RemovePrefix(string fullKey)
    {
        if (string.IsNullOrEmpty(_options.KeyPrefix))
        {
            return fullKey;
        }

        var prefix = _options.KeyPrefix.TrimEnd('/') + "/";
        return fullKey.StartsWith(prefix) ? fullKey.Substring(prefix.Length) : fullKey;
    }

    public void Dispose()
    {
        // Cancel all active watchers
        foreach (var watcher in _watchers.Values)
        {
            watcher.Cancel();
            watcher.Dispose();
        }
        _watchers.Clear();

        // Dispose semaphore
        _connectionSemaphore?.Dispose();
    }
}
