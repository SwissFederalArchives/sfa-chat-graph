using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Base;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages;
using AwosFramework.ApiClients.Jupyter.WebSocket.Parser;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Protocol
{
	[ProtocolImplementation("v1.kernel.websocket.jupyter.org")]
	public class V1KernelWebsocketJupyterOrgProtocol : IWebsocketProtocol
	{
		private static readonly RecyclableMemoryStreamManager _streamManager = new RecyclableMemoryStreamManager();
		private const int MIN_OFFSET_COUNT = 6;

		private ParserState _parserState;
		private readonly JsonSerializerOptions _jsonOptions;
		private readonly JupyterWebsocketOptions _options;
		private readonly List<ulong> _offsetList = new();

		public V1KernelWebsocketJupyterOrgProtocol(JupyterWebsocketOptions options)
		{
			_options = options;
			_parserState = new ParserState(options.ArrayPool, JsonSerializerOptions.Default, options.LoggerFactory);
			_jsonOptions = new JsonSerializerOptions();
			_jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
		}

		public async Task<ProtocolResult<WebsocketMessage, WebsocketError>> ReadAsync(Memory<byte> memory, bool endOfMessage)
		{
			int initialLength = memory.Length;
			do
			{
				var read = WebsocketFrameParser.Parse(memory.Span, ref _parserState);
				if (read == 0)
					return ProtocolResult.PartialResult(initialLength - memory.Length);

				memory = memory.Slice(read);
				if (_parserState.IsErrorState(out var errorCode))
				{
					var error = new WebsocketError(errorCode.Value, _parserState.Exception);
					return ProtocolResult.ErrorResult(error, initialLength - memory.Length);
				}

				if (_parserState.State == WebsocketFrameParserState.End)
					return ProtocolResult.CompletedResult(_parserState.PartialMessage, initialLength - memory.Length);
			} while (memory.Length > 0);

			return ProtocolResult.PartialResult(initialLength - memory.Length);
		}

		public async Task<long> SendAsync(WebsocketMessage msg, SendDeletegate sender)
		{
			_offsetList.Clear();
			var bufferCount = msg.Buffers?.Count ?? 0;
			int offsetCount = bufferCount + MIN_OFFSET_COUNT;
			using (var stream = _streamManager.GetStream())
			{
				stream.WriteUInt64Le((ulong)offsetCount);
				stream.Position += offsetCount*sizeof(ulong);
				_offsetList.Add((ulong)stream.Position);
				var channelName = msg.Channel.ToString().ToLower();
				await stream.WriteAsync(Encoding.UTF8.GetBytes(channelName));
				_offsetList.Add((ulong)stream.Position);
				await Extensions.SerializeNullableAsync(stream, msg.Header, _jsonOptions);
				_offsetList.Add((ulong)stream.Position);
				await Extensions.SerializeNullableAsync(stream, msg.ParentHeader, _jsonOptions);
				_offsetList.Add((ulong)stream.Position);
				await Extensions.SerializeNullableAsync(stream, msg.MetaData, _jsonOptions);
				_offsetList.Add((ulong)stream.Position);
				await Extensions.SerializeNullableAsync(stream, msg.Content, _jsonOptions);
				_offsetList.Add((ulong)stream.Position);
				var currentPos = stream.Position;

				if (msg.TransferableBuffers != null)
				{
					foreach (var bufferLen in msg.TransferableBuffers.BufferLengths.EmptyIfNull())
					{
						currentPos += bufferLen;
						_offsetList.Add((ulong)stream.Position);
					}
				}

				stream.Position = sizeof(ulong);
				foreach (var offset in _offsetList)
					stream.WriteUInt64Le(offset);

				var streamFitsMemory = stream.Length <= int.MaxValue;
				var memory = stream.GetBuffer().AsMemory(0, streamFitsMemory ? (int)stream.Length : int.MaxValue);
				await sender(memory, bufferCount == 0 && streamFitsMemory);
				if (streamFitsMemory == false)
				{
					stream.Position = int.MaxValue;
					var buffer = _options.ArrayPool.Rent(1024*1024*128);
					while (stream.Read(buffer) > 0)
						await sender(buffer, bufferCount == 0 && stream.Position == stream.Length);

					_options.ArrayPool.Return(buffer);
				}

				foreach (var (isLast, buffer) in msg.TransferableBuffers.EmptyIfNull().IsLast())
					await sender(buffer, isLast);

				for (int i = 0; i < bufferCount; i++)
				{
					var buffer = msg.Buffers![i];
				}

				return stream.Length;
			}
		}

		public void Dispose()
		{
			_parserState.Reset();
		}
	}
}
