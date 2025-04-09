using AwosFramework.ApiClients.Jupyter.WebSocket.Parser;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
	public class MessageTypeAttribute : Attribute
	{

		public static bool TryGetType(string protocolName, [NotNullWhen(true)]out Type? type) => ContentTypes.TryGetValue(protocolName, out type);

		public static FrozenDictionary<string, Type> ContentTypes { get; } =
			typeof(WebsocketFrameParser).Assembly
			.GetTypes()
			.SelectMany(x => x.GetCustomAttributes<MessageTypeAttribute>().Select(y => (attr: y, type: x)))
			.DistinctBy(x => x.attr.MessageType)
			.ToFrozenDictionary(x => x.attr.MessageType, x => x.type);

		public string MessageType { get; init; }
		public ChannelKind Channel { get; init; }

		public MessageTypeAttribute(string messageType, ChannelKind channel)
		{
			MessageType=messageType;
			Channel=channel;
		}
	}
}
