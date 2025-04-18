using System.Net.WebSockets;

namespace sfa_chat_graph.Server.Services.EventService
{
	public interface IEventProtocol<TEvent, TMessage>
	{
		public Task<TMessage> SerializeAsync(TEvent @event);
	}
}
