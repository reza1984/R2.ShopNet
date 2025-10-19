using System.Collections.Concurrent;
using Microsoft.Extensions.Primitives;

namespace R2.ShopNet.Gateway.API.Services;

/// <summary>
/// Provides change tokens to signal YARP configuration updates
/// </summary>
public sealed class ConfigurationChangeTokenSource : IDisposable
{
    private readonly ConcurrentQueue<CancellationTokenSource> _changeTokenSources = new();
    private CancellationTokenSource _currentTokenSource = new();

    /// <summary>
    /// Gets a change token that will be triggered when configuration changes
    /// </summary>
    public IChangeToken GetChangeToken()
    {
        var cts = new CancellationTokenSource();
        _changeTokenSources.Enqueue(cts);
        return new CancellationChangeToken(cts.Token);
    }

    /// <summary>
    /// Signals that the configuration has changed
    /// </summary>
    public void SignalChange()
    {
        var oldTokenSource = Interlocked.Exchange(ref _currentTokenSource, new CancellationTokenSource());
        
        // Cancel all pending change tokens
        while (_changeTokenSources.TryDequeue(out var tokenSource))
        {
            try
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Token source was already disposed
            }
        }

        oldTokenSource.Cancel();
        oldTokenSource.Dispose();
    }

    public void Dispose()
    {
        _currentTokenSource?.Dispose();
        
        while (_changeTokenSources.TryDequeue(out var tokenSource))
        {
            tokenSource.Dispose();
        }
    }
}
