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

		public static object? GetPrimitive(this JsonElement element)
		{
			switch (element.ValueKind)
			{
				case JsonValueKind.String:
					return element.GetString();

				case JsonValueKind.Number:
					if (element.TryGetInt32(out var intValue))
						return intValue;
					else if (element.TryGetUInt32(out var uintValue))
						return uintValue;
					else if (element.TryGetInt64(out var longValue))
						return longValue;
					else if (element.TryGetUInt64(out var ulongValue))
						return ulongValue;
					else if (element.TryGetDouble(out var doubleValue))
						return doubleValue;
					else
						return null;

				case JsonValueKind.True:
					return true;

				case JsonValueKind.False:
					return false;

				case JsonValueKind.Object:
				case JsonValueKind.Array:
					return element;

				case JsonValueKind.Null:
				case JsonValueKind.Undefined:
				default:
					return null;

			}
		}

		public static IEnumerable<(bool isLast, T item)> IsLast<T>(this IEnumerable<T> enumerable)
		{
			var enumerator = enumerable.GetEnumerator();
			if (enumerator.MoveNext() == false)
				yield break;
			
			T last = enumerator.Current;	
			while (enumerator.MoveNext())
			{
				yield return (false, last);
				last = enumerator.Current;
			}
			
			yield return (true, last);
		}

		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? enumerable) => enumerable ?? Enumerable.Empty<T>();

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
