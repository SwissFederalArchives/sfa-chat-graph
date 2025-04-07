using AwosFramework.ApiClients.Jupyter.WebSocket.Parser;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models
{
	public class WebsocketMessage : IDisposable
	{
		public ChannelKind Channel { get; internal set; }
		public MessageHeader? Header { get; internal set; }
		public MessageHeader? ParentHeader { get; internal set; }
		public JsonDocument? MetaData { get; internal set; }
		public object? Content { get; internal set; }
		public IBufferHolder? Buffers { get; internal set; }

		public void Dispose()
		{
			Buffers?.Dispose();
		}
	}
}
