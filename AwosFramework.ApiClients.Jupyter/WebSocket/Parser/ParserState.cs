using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Parser
{
	public struct ParserState
	{
		public const int BUFFERS_START_INDEX = 5;
		private readonly ArrayPool<byte> _arrayPool;

		public ParserState(ArrayPool<byte> arrayPool, JsonSerializerOptions? options = null, ILoggerFactory? loggerFactory = null)
		{
			_arrayPool = arrayPool;
			Logger = loggerFactory?.CreateLogger<ParserState>();
			PartialMessage = new WebsocketMessage();
			JsonOptions = new JsonSerializerOptions(options ?? JsonSerializerOptions.Default);
			JsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
		}

		public JsonSerializerOptions JsonOptions { get; init; }
		public ILogger? Logger { get; init; }
		public WebsocketFrameParserState State;
		public int OffsetCount;
		public int CurrentArrayIndex;
		public int CurrentBufferIndex;
		public int WorkingMemorySize;
		public byte[]? OffsetsRaw;
		public byte[]? WorkingMemory;
		public WebsocketMessage PartialMessage;
		public WebsocketParserError? ErrorCode;
		public Exception? Exception;
		public IWritableBufferHolder? Buffers;

		private int BufferCount => OffsetCount - 1 - BUFFERS_START_INDEX;

		public bool HasWorkingMemory => WorkingMemory != null;

		public Span<ulong> Offsets
		{
			get
			{
				if (OffsetsRaw == null)
					return default;

				return MemoryMarshal.Cast<byte, ulong>(OffsetsRaw).Slice(0, OffsetCount);
			}
		}

		public bool SetBuffers()
		{
			if (Buffers != null)
				return true;

			var bufferCount = BufferCount;
			if (bufferCount == 0)
			{
				Buffers = PooledBufferHolder.Empty;
				return true;
			}

			var offsets = Offsets;
			var start = offsets[BUFFERS_START_INDEX];
			var end = offsets[^1];
			var len = end - start;
			if (len > int.MaxValue)
			{
				SetError(WebsocketParserError.BufferTooLarge);
				return false;
			}

			Buffers = new PooledBufferHolder(bufferCount, (int)len, _arrayPool);
			PartialMessage.TransferableBuffers = Buffers;
			return true;
		}

		public void SetError(WebsocketParserError errorCode, Exception? exception = null)
		{
			Logger?.LogError(exception, "Websocket parser error: {ErrorCode}, current state: {State}", errorCode, State);
			State = WebsocketFrameParserState.Error;
			Exception = exception;
			ErrorCode = errorCode;
		}

		public bool IsErrorState([NotNullWhen(true)] out WebsocketParserError? error)
		{
			error = ErrorCode;
			return State == WebsocketFrameParserState.Error;
		}

		public byte[] RentWorkingMemory(int size)
		{
			WorkingMemorySize = size;
			if (WorkingMemory != null)
			{
				if (WorkingMemory.Length >= size)
					return WorkingMemory;

				_arrayPool.Return(WorkingMemory);
			}

			WorkingMemory = _arrayPool.Rent(size);
			return WorkingMemory;
		}

		public void Reset()
		{
			if (State == WebsocketFrameParserState.Error)
				PartialMessage.Dispose();

			PartialMessage = new WebsocketMessage();
			State = WebsocketFrameParserState.Start;
			WorkingMemorySize = 0;
			CurrentArrayIndex = 0;
			OffsetCount = 0;
			ErrorCode = null;
			Buffers = null;
			CurrentBufferIndex = 0;

			if (OffsetsRaw != null)
			{
				_arrayPool.Return(OffsetsRaw);
				OffsetsRaw = null;
			}

			if (WorkingMemory != null)
			{
				_arrayPool.Return(WorkingMemory);
				WorkingMemory = null;
			}
		}

		public bool RentOffsets()
		{
			var needed = OffsetCount*sizeof(long);
			if (int.MaxValue < needed)
				return false;

			if (OffsetsRaw != null)
			{
				if (OffsetsRaw.Length >= needed)
					return true;

				_arrayPool.Return(OffsetsRaw);
			}

			OffsetsRaw = _arrayPool.Rent(needed);
			return true;
		}

		public void NextState()
		{
			State++;
			if (State == WebsocketFrameParserState.Buffer && BufferCount == 0)
			{
				SetBuffers();
				State++;
			}

			CurrentArrayIndex = 0;
			WorkingMemorySize = 0;
		}

		public bool TryGetBufferLength(int index, out int length)
		{
			if (index > OffsetCount - 1)
			{
				length = 0;
				return false;
			}

			var offsets = Offsets;
			var start = offsets[index];
			var end = offsets[index+1];
			var longLength = end - start;
			if (int.MaxValue < longLength)
			{
				length = 0;
				return false;
			}

			length = (int)longLength;
			return true;
		}
	}
}
