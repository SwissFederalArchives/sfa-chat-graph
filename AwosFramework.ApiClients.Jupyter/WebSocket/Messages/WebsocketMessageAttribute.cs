using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Messages
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
	public class WebsocketMessageAttribute : Attribute
	{
		public string MessageType { get; init; }

		public WebsocketMessageAttribute(string messageType)
		{
			MessageType=messageType;
		}
	}
}
