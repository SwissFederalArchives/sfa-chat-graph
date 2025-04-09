using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages
{
	public class WebsocketMessage : IDisposable
	{
		[JsonPropertyName("channel")]
		public ChannelKind Channel { get; internal set; }

		[JsonPropertyName("header")]
		public MessageHeader? Header { get; internal set; }

		[JsonPropertyName("parent_header")]
		public MessageHeader? ParentHeader { get; internal set; }

		[JsonPropertyName("metadata")]
		public JsonDocument? MetaData { get; internal set; }

		[JsonPropertyName("content")]
		public object? Content { get; internal set; }

		[JsonPropertyName("buffers")]
		public ITransferableBufferHolder? TransferableBuffers { get; internal set; }

		public IBufferHolder Buffers => (IBufferHolder)TransferableBuffers;

		~WebsocketMessage()
		{
			Dispose();
		}

		public void Dispose()
		{
			Buffers?.Dispose();
		}
	}
}
