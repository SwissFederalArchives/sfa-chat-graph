using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Terminal.Protocol
{
	public class TerminalMessage
	{
		[SetsRequiredMembers]
		public TerminalMessage(TerminalMessageType type, params object?[] content)
		{
			this.MessageType = type;
			this.Content = content;
		}

		public required TerminalMessageType MessageType { get; set; }
		public object?[]? Content { get; set; }
	}
}
