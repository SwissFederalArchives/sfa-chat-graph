using AwosFramework.ApiClients.Jupyter.WebSocket.Base;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Json;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Protocol
{
	[ProtocolImplementation("jupyter", IsDefault = true)]
	public class JupyterProtocol : IJupyterProtocol
	{
		private readonly MemoryStream _readBuffer = new();
		private readonly MemoryStream _writeBuffer = new();
		private readonly JsonSerializerOptions _jsonOptions;
		private readonly JupyterWebsocketOptions _options;

		public JupyterProtocol(JupyterWebsocketOptions options)
		{
			_options = options;
			_jsonOptions = new JsonSerializerOptions();
			_jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
			_jsonOptions.Converters.Add(new BufferHolderConverter());
		}

		public void Dispose()
		{
			_readBuffer?.Dispose();
		}

		public async Task<ProtocolResult<JupyterMessage, JupyterError>> ReadAsync(Memory<byte> memory, bool endOfMessage)
		{
			try
			{
				await _readBuffer.WriteAsync(memory);
				if (endOfMessage)
				{
					_readBuffer.Position = 0;
					var msg = await JsonSerializer.DeserializeAsync<JupyterMessage>(_readBuffer, _jsonOptions);
					_readBuffer.SetLength(0);

					if (msg != null)
						return ProtocolResult.CompletedResult(msg, memory.Length);
					else
						return ProtocolResult.PartialResult(memory.Length);
				}
				else
				{
					return ProtocolResult.PartialResult(memory.Length);
				}
			}
			catch (Exception ex)
			{
				_readBuffer.SetLength(0);
				return ProtocolResult.ErrorResult(new JupyterError(WebsocketParserError.Unknown, ex), memory.Length);
			}
		}

		public async Task<long> SendAsync(JupyterMessage toSend, SendDeletegate sender)
		{
			_writeBuffer.SetLength(0);
			await JsonSerializer.SerializeAsync(_writeBuffer, toSend, _jsonOptions);

			var streamFitsMemory = _writeBuffer.Length <= int.MaxValue;
			var memory = _writeBuffer.GetBuffer().AsMemory(0, streamFitsMemory ? (int)_writeBuffer.Length : int.MaxValue);
			await sender(memory, streamFitsMemory);
			if (streamFitsMemory == false)
			{
				_writeBuffer.Position = int.MaxValue;
				var buffer = _options.ArrayPool.Rent(1024*1024*128);
				while (_writeBuffer.Read(buffer) > 0)
					await sender(buffer, _writeBuffer.Position == _writeBuffer.Length);

				_options.ArrayPool.Return(buffer);
			}

			return _writeBuffer.Length;
		}
	}
}
