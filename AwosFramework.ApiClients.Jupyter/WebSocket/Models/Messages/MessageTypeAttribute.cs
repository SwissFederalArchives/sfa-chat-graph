using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
	public class MessageTypeAttribute : Attribute
	{
		public string MessageType { get; init; }
		public ChannelKind Channel { get; init; }

		public MessageTypeAttribute(string messageType, ChannelKind channel)
		{
			MessageType=messageType;
			Channel=channel;
		}
	}
}
