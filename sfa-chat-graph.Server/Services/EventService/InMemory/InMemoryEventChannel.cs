using Microsoft.IO;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace sfa_chat_graph.Server.Services.EventService.InMemory
{
	public class InMemoryEventChannel<TChannel, TEvent, TTarget, TMessage> : IAsyncDisposable, IEventChannel<TChannel, TEvent, TTarget, TMessage>
		where TEvent : IEvent
		where TTarget : IEventTarget<TMessage>
	{

		public TChannel ChannelKey { get; init; }
		public bool Disposed { get; private set; }

		private TEvent _lastEvent;
		private readonly Channel<TEvent> _channel;
		private readonly ConcurrentDictionary<long, TTarget> _targets;
		private readonly CancellationTokenSource _cts = new CancellationTokenSource();
		private readonly IComparer<TEvent> _comparer;
		private readonly IEventProtocol<TEvent, TMessage> _protocol;
		private readonly ILogger _logger;
		private readonly Task _sendTask;
		public Task Task => _sendTask;

		public event Action<TChannel, TTarget> OnTargetAdded;
		public event Action<TChannel, TEvent> OnEventSent;
		public event Action<TChannel, TTarget> OnSocketRemoved;
		public event Action<TChannel> NoTargets;

		public InMemoryEventChannel(TChannel channelKey, IEventProtocol<TEvent, TMessage> protocol, ILoggerFactory loggerFactory, IComparer<TEvent> comparer = null)
		{
			ChannelKey = channelKey;
			_comparer = comparer ?? Comparer<TEvent>.Default;
			_lastEvent = default;
			_targets = new ConcurrentDictionary<long, TTarget>();
			_channel = Channel.CreateUnbounded<TEvent>();
			_protocol = protocol;
			_logger = loggerFactory.CreateLogger($"{nameof(InMemoryEventChannel<TChannel, TEvent, TTarget, TMessage>)}[{ChannelKey}]");
			_sendTask = SendLoopAsync();
		}

		private void ThrowIfDisposed()
		{
			if (Disposed)
				throw new ObjectDisposedException(nameof(InMemoryEventChannel<TChannel, TEvent, TTarget, TMessage>));
		}

		private void RemoveTarget(long id)
		{
			if (_targets.TryRemove(id, out var target))
			{
				OnSocketRemoved?.Invoke(ChannelKey, target);
				_logger.LogDebug("Target removed: {TargetId}", id);
				if (_targets.IsEmpty)
				{
					NoTargets?.Invoke(ChannelKey);
					_logger.LogInformation("Channel {ChannelKey} has no targets", ChannelKey);
				}
			}
		}

		private void HandleTargetClose(Task<IEventTarget<TMessage>> target)
		{
			if (target.IsFaulted)
			{
				_logger.LogError(target.Exception, "Error while waiting for target close");
				return;
			}

			if (target.Result != null)
				RemoveTarget(target.Result.Id);
		}

		public void RegisterTarget(TTarget target)
		{
			ThrowIfDisposed();
			if (_targets.TryAdd(target.Id, target))
			{
				target.WaitForCloseAsync(_cts.Token).ContinueWith(HandleTargetClose);
				OnTargetAdded?.Invoke(ChannelKey, target);
				_logger.LogDebug("Target added: {TargetId}", target.Id);
			}
		}

		private async Task SendLoopAsync()
		{
			var watch = new Stopwatch();
			while (_cts.IsCancellationRequested == false)
			{
				var @event = await _channel.Reader.ReadAsync(_cts.Token);
				if (_comparer.Compare(@event, _lastEvent) > 0)
				{
					_lastEvent = @event;
					_logger.LogDebug("Sending event: {Event}", @event);
					watch.Restart();
					var message = await _protocol.SerializeAsync(@event);
					var tasks = _targets.Values.Select(x => x.SendAsync(message));
					await foreach (var task in Task.WhenEach(tasks))
					{
						if (task.Result.error)
							RemoveTarget(task.Result.target.Id);
					}

					watch.Stop();
					_logger.LogDebug("Event sent in {Elapsed}ms", watch.ElapsedMilliseconds);
					OnEventSent?.Invoke(ChannelKey, @event);
				}
			}
		}

		private async Task CloseAndDisposeAsync(TTarget target)
		{
			await target.CloseAsync();
			await target.DisposeAsync();
		}

		public async ValueTask DisposeAsync()
		{
			if (Disposed)
				return;

			Disposed = true;
			_cts.Cancel();
			var tasks = _targets.Values.Select(CloseAndDisposeAsync);
			await Task.WhenAll(tasks);
		}

		public async Task PushAsync(TEvent @event)
		{
			if (Disposed == false)
				await _channel.Writer.WriteAsync(@event, _cts.Token);
		}
	}
}
