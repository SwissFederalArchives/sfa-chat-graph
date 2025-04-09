using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser
{

	public static class WebsocketFrameParser
	{
		public static int Parse(Span<byte> data, ref ParserState state)
		{
			try
			{
				if (state.State == WebsocketFrameParserState.End || state.State == WebsocketFrameParserState.Error)
					state.Reset();

				if (state.State == WebsocketFrameParserState.Start)
					state.State = WebsocketFrameParserState.ReadOffsetCount;

				int countRead = 0;
				var handler = StateHandlers[(int)state.State];
				if (handler?.Invoke(ref state, data, ref countRead) == true)
					state.NextState();

				return countRead;
			}
			catch (Exception ex)
			{
				state.SetError(WebsocketParserError.Unknown, ex);
				return 0;
			}
		}

		private delegate bool TryHandleDelegate(ref ParserState parser, Span<byte> data, ref int countRead);
		private static readonly TryHandleDelegate?[] StateHandlers =
		{
			null,
			TryReadOffsetCount,
			TryReadOffsets,
			TryReadChannel,
			TryReadHeader,
			TryReadParentHeader,
			TryReadMetaData,
			TryReadContent,
			TryReadBuffer,
			null
		};


		private static unsafe bool TryReadOffsetCount(ref ParserState state, Span<byte> data, ref int countRead)
		{
			if (data.Length < 8)
				return false;

			countRead = 8;
			var count = BitConverter.ToUInt64(data);
			if (int.MaxValue < count)
			{
				state.SetError(WebsocketParserError.OffsetCountTooLarge);
				return false;
			}

			state.OffsetCount = (int)count;
			return true;
		}

		private static unsafe bool TryReadOffsets(ref ParserState state, Span<byte> data, ref int countRead)
		{
			if (state.RentOffsets() == false)
			{
				state.SetError(WebsocketParserError.MessageToLarge);
				return false;
			}

			var readable = data.Length / sizeof(ulong);
			var offsets = state.Offsets;
			while (data.Length > sizeof(ulong) && state.CurrentArrayIndex < state.OffsetCount)
			{
				offsets[state.CurrentArrayIndex++] = BinaryPrimitives.ReadUInt64LittleEndian(data);
				data = data.Slice(sizeof(ulong));
				countRead += sizeof(long);
			}

			return state.CurrentArrayIndex == state.OffsetCount;
		}

		private static bool WaitForFullBuffer(int bufferIndex, ref ParserState state, Span<byte> data, ref int countRead, out Span<byte> buffer)
		{
			if (state.TryGetBufferLength(bufferIndex, out var length) == false)
			{
				state.SetError(WebsocketParserError.BufferTooLarge);
				buffer = default;
				return false;
			}

			if (state.HasWorkingMemory == false && data.Length >= length)
			{
				buffer = data.Slice(0, length);
				countRead = length;
				return true;
			}
			else
			{
				if (state.HasWorkingMemory == false)
					state.RentWorkingMemory(length);

				var remaining = Math.Min(data.Length, length - state.CurrentArrayIndex);
				data.CopyTo(state.WorkingMemory.AsSpan(state.CurrentArrayIndex, remaining));
				state.CurrentArrayIndex += remaining;
				countRead = remaining;
				if (state.CurrentArrayIndex < state.WorkingMemorySize)
				{
					buffer = default;
					return false;
				}
				else
				{
					buffer = state.WorkingMemory.AsSpan(0, length);
					return true;
				}
			}
		}

		private static bool TrySkipBuffer(int bufferIndex, ref ParserState state, Span<byte> data, ref int countRead)
		{
			if (state.TryGetBufferLength(bufferIndex, out var length) == false)
			{
				state.SetError(WebsocketParserError.BufferTooLarge);
				return false;
			}

			countRead = Math.Min(data.Length, length - state.CurrentArrayIndex);
			state.CurrentArrayIndex += countRead;
			return state.CurrentArrayIndex >= length;
		}

		private static unsafe bool TryReadChannel(ref ParserState state, Span<byte> data, ref int countRead)
		{
			if (WaitForFullBuffer(0, ref state, data, ref countRead, out var span) == false)
				return false;

			var count = Encoding.UTF8.GetMaxCharCount(span.Length);
			Span<char> chars = stackalloc char[count];
			var charCount = Encoding.UTF8.GetChars(span, chars);
			if (Enum.TryParse<ChannelKind>(chars.Slice(0, charCount), true, out var channelKind) == false)
			{
				state.SetError(WebsocketParserError.UnknownChannel);
				return false;
			}

			state.PartialMessage.Channel = channelKind;
			return true;
		}

		private static unsafe bool TryReadHeader(ref ParserState state, Span<byte> data, ref int countRead)
		{
			if (WaitForFullBuffer(1, ref state, data, ref countRead, out var span) == false)
				return false;

			try
			{
				state.PartialMessage.Header = JsonSerializer.Deserialize<MessageHeader>(span, state.JsonOptions);
			}
			catch (JsonException ex)
			{
				state.SetError(WebsocketParserError.MalformedHeader, ex);
				return false;
			}

			return true;
		}

		private static unsafe bool TryReadParentHeader(ref ParserState state, Span<byte> data, ref int countRead)
		{
			if (WaitForFullBuffer(2, ref state, data, ref countRead, out var span) == false)
				return false;

			try
			{
				state.PartialMessage.ParentHeader = JsonSerializer.Deserialize<MessageHeader>(span, state.JsonOptions);
			}
			catch (JsonException ex)
			{
				state.SetError(WebsocketParserError.MalformedHeader, ex);
				return false;
			}

			return true;
		}

		private static unsafe bool TryReadMetaData(ref ParserState state, Span<byte> data, ref int countRead)
		{
			if (WaitForFullBuffer(3, ref state, data, ref countRead, out var span) == false)
				return false;

			try
			{
				state.PartialMessage.MetaData = JsonSerializer.Deserialize<JsonDocument>(span, state.JsonOptions);
			}
			catch (JsonException ex)
			{
				state.SetError(WebsocketParserError.MalformedMetadata, ex);
				return false;
			}

			return true;
		}

		private static unsafe bool TryReadContent(ref ParserState state, Span<byte> data, ref int countRead)
		{
			if (MessageTypeAttribute.TryGetType(state.PartialMessage.Header!.MessageType, out var messageType))
			{
				if (WaitForFullBuffer(4, ref state, data, ref countRead, out var span) == false)
					return false;

				try
				{
					state.PartialMessage.Content = JsonSerializer.Deserialize(span, messageType, state.JsonOptions);
				}
				catch (JsonException ex)
				{
					state.SetError(WebsocketParserError.MalformedContent, ex);
					return false;
				}

				return true;
			}
			else
			{
				return TrySkipBuffer(4, ref state, data, ref countRead);
			}
		}

		private static unsafe bool TryReadBuffer(ref ParserState state, Span<byte> data, ref int countRead)
		{
			if (state.SetBuffers() == false)
				return false;

			if (state.Buffers.Count == 0)
			{
				countRead = 0;
				return true;
			}

			while (countRead < data.Length && state.CurrentBufferIndex < state.Buffers.Count)
			{
				if (state.TryGetBufferLength(state.CurrentBufferIndex + ParserState.BUFFERS_START_INDEX, out var length) == false)
				{
					state.SetError(WebsocketParserError.BufferTooLarge);
					return false;
				}

				var write = state.Buffers.WriteAccess(state.CurrentBufferIndex, length);
				var countWrite = Math.Min(data.Length, length - state.CurrentArrayIndex);
				data.Slice(state.CurrentArrayIndex, countWrite).CopyTo(write.Span);
				countRead += countWrite;
				state.CurrentArrayIndex += countWrite;
				if (state.CurrentArrayIndex >= length)
				{
					state.CurrentBufferIndex++;
					state.CurrentArrayIndex = 0;
				}
			}

			return state.CurrentBufferIndex >= state.Buffers.Count;
		}

	}
}
