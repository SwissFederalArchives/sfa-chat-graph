using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Base;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Protocol;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Terminal.Protocol
{
	[ProtocolImplementation("terminal", IsDefault = true)]
	class TerminalWebsocketProtocol : ITerminalProtocol
	{
		private static readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager = new();
		private readonly JsonSerializerOptions _jsonOptions;
		private readonly RecyclableMemoryStream _readStream;
		private readonly TerminalWebsocketClientOptions _options;

		public TerminalWebsocketProtocol(TerminalWebsocketClientOptions options)
		{
			_options = options;
			_jsonOptions = new JsonSerializerOptions();
			_jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
			_readStream = _recyclableMemoryStreamManager.GetStream();
		}

		public void Dispose()
		{
			_readStream?.Dispose();
		}

		public async Task<ProtocolResult<TerminalMessage, TerminalError>> ReadAsync(Memory<byte> memory, bool endOfMessage)
		{
			await _readStream.WriteAsync(memory);
			if (endOfMessage)
			{
				try
				{
					_readStream.Position = 0;
					var elements = await JsonSerializer.DeserializeAsync<JsonElement[]>(_readStream, _jsonOptions);
					var components = elements.Select(x => x.GetPrimitive()).ToArray();
					_readStream.SetLength(0);
					var type = JsonNamingPolicy.CamelCase.ConvertName((string)components[0]);
					if (Enum.TryParse<TerminalMessageType>(type, true, out var messageType) == false)
						messageType = TerminalMessageType.Unknown;

					var msg = new TerminalMessage(messageType, components[1..]);
					return ProtocolResult.CompletedResult(msg, memory.Length);
				}
				catch (Exception ex)
				{
					return ProtocolResult.ErrorResult(TerminalError.FromException(ex), memory.Length);
				}
			}
			else
			{
				return ProtocolResult.TerminalPartialResult(memory.Length);
			}
		}

		public async Task<long> SendAsync(TerminalMessage toSend, SendDeletegate sender)
		{
			object[] data = new object[(toSend.Content?.Length ?? 0) + 1];
			data[0] = toSend.MessageType.ToString().ToLower();
			toSend.Content?.CopyTo(data, 1);
			var message = JsonSerializer.Serialize(data, _jsonOptions);
			var bytes = Encoding.UTF8.GetBytes(message);
			await sender(bytes, true);
			return bytes.Length;
		}
	}
}
