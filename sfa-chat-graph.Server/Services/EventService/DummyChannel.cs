namespace SfaChatGraph.Server.Services.EventService
{
	public class DummyChannel<TChannel, TEvent, TTarget, TMessage> : IEventChannel<TChannel, TEvent, TTarget, TMessage> where TEvent : IEvent where TTarget : IEventTarget<TMessage>
	{
		private IEventChannel<TChannel, TEvent, TTarget, TMessage> _channel;
		public TChannel ChannelKey { get; init; }

		public DummyChannel(TChannel channelKey)
		{
			ChannelKey = channelKey;
		}

		internal void PromoteToProxy(IEventChannel<TChannel, TEvent, TTarget, TMessage> channel)
		{
			if (_channel != null)
				throw new InvalidOperationException("Dummy Channel already promoted to proxy.");

			if(channel.ChannelKey.Equals(ChannelKey) == false)
				throw new InvalidOperationException($"Channel key mismatch. Expected {ChannelKey}, but got {channel.ChannelKey}.");

			_channel = channel;
		}

		public Task PushAsync(TEvent @event) => _channel?.PushAsync(@event) ?? Task.CompletedTask;

		public void RegisterTarget(TTarget sink)
		{
			throw new InvalidOperationException("Dummy Channel cannot register target.");
		}
	}
}
