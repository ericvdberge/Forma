using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Forma.Application.Plugins;

/// <summary>Sends notification events to plugins, in the background, after the change they describe is stored.</summary>
public interface IPluginEventPublisher
{
    /// <summary>Queues <paramref name="data"/> as <paramref name="eventName"/>. Returns at once; never throws for plugin problems.</summary>
    void Publish(string eventName, object data);
}

/// <summary>
/// An in-memory queue: events are lost when the API stops before they ran. Good enough for notifications that only
/// inform plugins; delivery that must survive restarts would need an outbox table (<c>docs/design/plugins.md</c> § 8).
/// </summary>
public sealed class PluginEventQueue(TimeProvider timeProvider, ILogger<PluginEventQueue> logger) : IPluginEventPublisher
{
    /// <summary>Events waiting at most; past this new events are dropped (and logged) rather than slowing requests down.</summary>
    public const int Capacity = 1000;

    private readonly Channel<PluginEventEnvelope> _channel = Channel.CreateBounded<PluginEventEnvelope>(
        new BoundedChannelOptions(Capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

    public ChannelReader<PluginEventEnvelope> Reader => _channel.Reader;

    public void Publish(string eventName, object data)
    {
        var envelope = new PluginEventEnvelope(eventName, Guid.NewGuid(), timeProvider.GetUtcNow().UtcDateTime, data);
        if (!_channel.Writer.TryWrite(envelope))
        {
            logger.LogWarning("The plugin event queue is full; {Event} {EventId} was dropped", eventName, envelope.Id);
        }
    }
}

/// <summary>Takes events off the <see cref="PluginEventQueue"/> and runs the plugins subscribed to them.</summary>
public sealed class PluginEventDispatcher(PluginEventQueue queue, IPluginRunner runner, ILogger<PluginEventDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Plugin code is synchronous (a WebAssembly call); keep it off the thread that started the host.
        await Task.Yield();
        try
        {
            await foreach (var envelope in queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    runner.Run(envelope);
                }
                catch (Exception e)
                {
                    // The sandbox reports plugin failures as results; this is a bug on our side. Keep dispatching.
                    logger.LogError(e, "Dispatching {Event} {EventId} to plugins failed", envelope.Event, envelope.Id);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }
}
