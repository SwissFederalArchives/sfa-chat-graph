using System.Net.WebSockets;

namespace SfaChatGraph.Server.Services.EventService
{
	public interface IEventProtocol<TEvent, TMessage>
	{
		public Task<TMessage> SerializeAsync(TEvent @event);
	}
}
