using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser
{
	public enum WebsocketFrameParserState
	{
		Start,
		ReadOffsetCount,
		ReadOffsets,
		ReadChannel,
		Header,
		ParentHeader,
		MetaData,
		Content,
		Buffer,
		End,
		Error
	}
}
