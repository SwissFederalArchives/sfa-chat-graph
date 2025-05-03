using Lucene.Net.Analysis.Sinks;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace SfaChatGraph.Server.Services.EventService.InMemory
{
	public class InMemoryEventService<TChannel, TEvent, TTarget, TMessage> : IEventService<TChannel, TEvent, TTarget, TMessage> where TEvent : IEvent where TTarget : IEventTarget<TMessage>
	{
		private readonly ConcurrentDictionary<TChannel, IEventChannel<TChannel, TEvent, TTarget, TMessage>> _channels;
		private readonly IEventProtocol<TEvent, TMessage> _eventProtocol;
		private readonly ILoggerFactory _loggerFactory;

		public InMemoryEventService(IEventProtocol<TEvent, TMessage> eventProtocol, ILoggerFactory loggerFactory)
		{
			_eventProtocol=eventProtocol;
			_channels = new();
			_loggerFactory=loggerFactory;
		}

		private IEventChannel<TChannel, TEvent, TTarget, TMessage> CreateDummyChannel(TChannel key) => new DummyChannel<TChannel, TEvent, TTarget, TMessage>(key);

		public IEventChannel<TChannel, TEvent, TTarget, TMessage> GetChannel(TChannel key) => _channels.GetOrAdd(key, CreateDummyChannel);

		private IEventChannel<TChannel, TEvent, TTarget, TMessage> UpdateChannel(TChannel key, IEventChannel<TChannel, TEvent, TTarget, TMessage> channel)
		{
			if (channel is DummyChannel<TChannel, TEvent, TTarget, TMessage> dummyChannel)
			{
				var newChannel = CreateChannel(key);
				dummyChannel.PromoteToProxy(newChannel);
				return newChannel;
			}
			else
			{
				return channel;
			}
		}

		private InMemoryEventChannel<TChannel, TEvent, TTarget, TMessage> CreateChannel(TChannel key)
		{
			var channel = new InMemoryEventChannel<TChannel, TEvent, TTarget, TMessage>(key, _eventProtocol, _loggerFactory);
			return channel;
		}

		public void RegisterTarget(TChannel key, TTarget target)
		{
			var channel = _channels.AddOrUpdate(key, CreateChannel, UpdateChannel);
			channel.RegisterTarget(target);
		}

		public Task PushAsync<T>(T keyedEvent) where T : TEvent, IKeyedEvent<TChannel>
		{
			var channel = GetChannel(keyedEvent.Key);
			return channel?.PushAsync(keyedEvent);
		}

	}
}
