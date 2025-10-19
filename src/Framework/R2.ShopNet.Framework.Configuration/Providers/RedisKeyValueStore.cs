using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace R2.ShopNet.Framework.Configuration.Providers;

/// <summary>
/// Redis implementation of key-value store
/// High-performance distributed cache with pub/sub for watching
/// </summary>
public class RedisKeyValueStore : IKeyValueStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    private readonly RedisKeyValueOptions _options;
    private readonly ILogger<RedisKeyValueStore> _logger;

    public RedisKeyValueStore(
        IConnectionMultiplexer redis,
        IOptions<RedisKeyValueOptions> options,
        ILogger<RedisKeyValueStore> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _database = _redis.GetDatabase(_options.Database);
        _subscriber = _redis.GetSubscriber();
    }

    /// <summary>
    /// Get full Redis key with prefix
    /// </summary>
    private string GetFullKey(string key)
    {
        if (string.IsNullOrEmpty(_options.KeyPrefix))
        {
            return key;
        }

        return $"{_options.KeyPrefix}{key}";
    }

    /// <summary>
    /// Get value by key
    /// </summary>
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var value = await _database.StringGetAsync(fullKey);
            
            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Key {Key} not found in Redis", fullKey);
                return null;
            }

            return value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key {Key} from Redis", key);
            throw;
        }
    }

    /// <summary>
    /// Get multiple values by keys
    /// </summary>
    public async Task<IDictionary<string, string>> GetManyAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var redisKeys = keys.Select(k => (RedisKey)GetFullKey(k)).ToArray();
            var values = await _database.StringGetAsync(redisKeys);

            var result = new Dictionary<string, string>();
            var keyArray = keys.ToArray();

            for (int i = 0; i < keyArray.Length; i++)
            {
                if (!values[i].IsNullOrEmpty)
                {
                    result[keyArray[i]] = values[i].ToString()!;
                }
            }

            _logger.LogDebug("Retrieved {Count} keys from Redis", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple keys from Redis");
            throw;
        }
    }

    /// <summary>
    /// Get all values with a key prefix (using SCAN for efficiency)
    /// </summary>
    public async Task<IDictionary<string, string>> GetByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPrefix = GetFullKey(prefix);
            var pattern = $"{fullPrefix}*";
            var result = new Dictionary<string, string>();

            // Use SCAN instead of KEYS for production safety
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var value = await _database.StringGetAsync(key);
                if (!value.IsNullOrEmpty)
                {
                    // Remove prefix from returned key
                    var originalKey = key.ToString();
                    if (!string.IsNullOrEmpty(_options.KeyPrefix) && originalKey.StartsWith(_options.KeyPrefix))
                    {
                        originalKey = originalKey.Substring(_options.KeyPrefix.Length);
                    }
                    result[originalKey] = value.ToString()!;
                }
            }

            _logger.LogDebug("Retrieved {Count} keys with prefix {Prefix}", result.Count, prefix);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys by prefix {Prefix} from Redis", prefix);
            throw;
        }
    }

    /// <summary>
    /// Set a key-value pair with optional expiration
    /// </summary>
    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var expiry = _options.DefaultExpiration.HasValue 
                ? TimeSpan.FromSeconds(_options.DefaultExpiration.Value) 
                : (TimeSpan?)null;

            await _database.StringSetAsync(fullKey, value, expiry);
            
            // Publish change notification for watchers
            await _subscriber.PublishAsync(
                RedisChannel.Literal($"__keyspace@{_options.Database}__:{fullKey}"),
                "set");

            _logger.LogDebug("Set key {Key} in Redis", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key {Key} in Redis", key);
            throw;
        }
    }

    /// <summary>
    /// Set multiple key-value pairs
    /// </summary>
    public async Task SetManyAsync(
        IDictionary<string, string> keyValues,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = keyValues.Select(kvp => SetAsync(kvp.Key, kvp.Value, cancellationToken));
            await Task.WhenAll(tasks);
            
            _logger.LogDebug("Set {Count} keys in Redis", keyValues.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple keys in Redis");
            throw;
        }
    }

    /// <summary>
    /// Delete a key
    /// </summary>
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            await _database.KeyDeleteAsync(fullKey);
            
            _logger.LogDebug("Deleted key {Key} from Redis", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key {Key} from Redis", key);
            throw;
        }
    }

    /// <summary>
    /// Delete all keys with a prefix
    /// </summary>
    public async Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPrefix = GetFullKey(prefix);
            var pattern = $"{fullPrefix}*";
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            var keysToDelete = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                keysToDelete.Add(key);
            }

            if (keysToDelete.Count > 0)
            {
                await _database.KeyDeleteAsync(keysToDelete.ToArray());
                _logger.LogDebug("Deleted {Count} keys with prefix {Prefix}", keysToDelete.Count, prefix);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting keys by prefix {Prefix} from Redis", prefix);
            throw;
        }
    }

    /// <summary>
    /// Check if a key exists
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            return await _database.KeyExistsAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key {Key} exists in Redis", key);
            throw;
        }
    }

    /// <summary>
    /// Watch for changes to a key using Redis Pub/Sub
    /// Note: Requires Redis keyspace notifications to be enabled:
    /// CONFIG SET notify-keyspace-events KEA
    /// </summary>
    public async Task WatchAsync(
        string key,
        Action<string?> onChanged,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var channel = RedisChannel.Literal($"__keyspace@{_options.Database}__:{fullKey}");

            await _subscriber.SubscribeAsync(channel, async (ch, message) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                try
                {
                    // Get the current value when a change is detected
                    var value = await _database.StringGetAsync(fullKey);
                    onChanged(value.IsNullOrEmpty ? null : value.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in watch callback for key {Key}", fullKey);
                }
            });

            _logger.LogDebug("Started watching key {Key} in Redis", fullKey);

            // Keep subscription alive until cancellation
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }

                await _subscriber.UnsubscribeAsync(channel);
                _logger.LogDebug("Stopped watching key {Key} in Redis", fullKey);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error watching key {Key} in Redis", key);
            throw;
        }
    }

    public void Dispose()
    {
        // Redis ConnectionMultiplexer is typically registered as singleton
        // and managed by DI container, so we don't dispose it here
        _logger.LogDebug("RedisKeyValueStore disposed");
    }
}
