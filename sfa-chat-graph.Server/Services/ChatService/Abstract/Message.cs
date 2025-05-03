using SfaChatGraph.Server.Models;

namespace SfaChatGraph.Server.Services.ChatService.Abstract
{
	public class Message<TMessage>
	{
		public Message(TMessage message, ApiMessage apiMessage)
		{
			InternalMessage = message;
			ApiMessage = apiMessage;
		}

		public TMessage InternalMessage { get; }
		public ApiMessage ApiMessage { get; }
	}
}
