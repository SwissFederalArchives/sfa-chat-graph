using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Utils
{
	public static class Extensions
	{
		public static IEnumerable<(int index, T item)> Enumerate<T>(this IEnumerable<T> enumerable, int offset = 0)
		{
			int i = offset;
			foreach (var x in enumerable.Skip(offset))
				yield return (i++, x);
		}

		public static async Task SerializeNullableAsync<T>(Stream stream, T? value, JsonSerializerOptions options) where T : class
		{
			if (value == null)
			{
				stream.Write(Encoding.ASCII.GetBytes("{}"));
			}
			else
			{
				await JsonSerializer.SerializeAsync<T>(stream, value, options);
			}
		}

		public static Uri OfComponents(this Uri uri, UriComponents components) => new Uri(uri.GetComponents(components, UriFormat.UriEscaped), uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);

		public static async Task WaitForEndOfMessageAsync(this ClientWebSocket websocket, Memory<byte> buffer, CancellationToken token)
		{
			ValueWebSocketReceiveResult result = default;
			do
			{
				result = await websocket.ReceiveAsync(buffer, token);
			} while (result.EndOfMessage == false);
		}

		public static void WriteUInt64Le(this Stream stream, ulong value)
		{
			Span<byte> buffer = stackalloc byte[sizeof(ulong)];
			BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
			stream.Write(buffer);
		}

		public static string ToSnakeCase(this string @string, string join = "_")
		{
			var upperCase = @string.Enumerate(1).Where(x => char.IsUpper(x.item)).Select(x => x.index);
			if (upperCase.Any() == false)
				return @string.ToLower();

			var res = new StringBuilder();
			int lastIndex = 0;
			foreach (var index in upperCase)
			{
				res.Append(@string[lastIndex..index].ToLower());
				res.Append(join);
				lastIndex = index;
			}

			res.Append(@string[lastIndex..]);
			return res.ToString();
		}
	}
}
