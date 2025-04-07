using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Parser
{
	public enum WebsocketParserError
	{
		Unknown,
		OffsetCountTooLarge,
		MessageToLarge,
		BufferTooLarge,
		UnknownChannel,
		MalformedHeader,
		MalformedMetadata,
		MalformedContent
	}
}
