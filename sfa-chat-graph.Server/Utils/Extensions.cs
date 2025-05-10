using AwosFramework.Generators.FunctionCalling;
using Json.More;
using Json.Schema;
using MessagePack;
using OpenAI.Chat;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.Utils;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.Utils
{
	public static class Extensions
	{
		public static IEnumerable<TResult> SelectWhere<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> mapper, Func<TSource, bool> predicate) => enumerable.Where(predicate).Select(mapper);
		public static IEnumerable<TResult> SelectNonNull<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> mapper) => enumerable.SelectWhere(mapper, x => x!= null);

		public static IEnumerable<KeyValuePair<TKey, TValue>> ZipPair<TKey, TValue>(this IEnumerable<TKey> enumerable, IEnumerable<TValue> other)
		{
			using var enumerator1 = enumerable.GetEnumerator();
			using var enumerator2 = other.GetEnumerator();
			while (enumerator1.MoveNext() && enumerator2.MoveNext())
				yield return new KeyValuePair<TKey, TValue>(enumerator1.Current, enumerator2.Current);

		}

		public static IEnumerable<TTo> Select<TTo, TFrom, TArg>(this IEnumerable<TFrom> enumerable, Func<TFrom, TArg, TTo> map, TArg arg1) => enumerable.Select(x => map(x, arg1));
		public static IEnumerable<TTo> Select<TTo, TFrom, TArg, TArg2>(this IEnumerable<TFrom> enumerable, Func<TFrom, TArg, TArg2, TTo> map, TArg arg1, TArg2 arg2) => enumerable.Select(x => map(x, arg1, arg2));
		public static IEnumerable<TTo> Select<TTo, TFrom, TArg, TArg2, TArg3>(this IEnumerable<TFrom> enumerable, Func<TFrom, TArg, TArg2, TArg3, TTo> map, TArg arg1, TArg2 arg2, TArg3 arg3) => enumerable.Select(x => map(x, arg1, arg2, arg3));

		public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
		{
			foreach (var item in enumerable)
				action(item);
		}

		public static void WriteStringArray(this ref MessagePackWriter writer, IEnumerable<string> array)
		{
			writer.WriteArrayHeader(array.Count());
			foreach (var item in array)
				writer.Write(item);
		}

		public static string[] ReadStringArray(this ref MessagePackReader reader)
		{
			var len = reader.ReadArrayHeader();
			var array = new string[len];
			for (int i = 0; i < len; i++)
				array[i] = reader.ReadString();

			return array;
		}

		public static Uri ReadUri(this ref MessagePackReader reader)
		{
			var uri = reader.ReadString();
			return new Uri(uri);
		}

		public static string ToIriList(this IEnumerable<string> iris) => string.Join(" ", iris.Select(x => $"<{x}>"));

		public static void AddRange<T>(this ICollection<T> ts, IEnumerable<T> items)
		{
			foreach (var item in items)
				ts.Add(item);
		}

		public static T MaxOrDefault<T>(this IEnumerable<T> enumerable, T defaultNumber = default) where T : INumber<T>
		{
			if(enumerable.Any() == false)
				return defaultNumber;

			return enumerable.Max();
		}

		public static TNumber MaxOrDefault<T, TNumber>(this IEnumerable<T> enumerable, Func<T, TNumber> selector, TNumber defaultNumber = default) where TNumber : INumber<TNumber>
		{
			if (enumerable.Any() == false)
				return defaultNumber;

			return enumerable.Max(selector);
		}

		public static string Ellipsis(this string @string, int maxLen, string end = "...")
		{
			var len = maxLen - end.Length;
			if (@string.Length > len)
				return @string.Substring(0, len) + end;

			return @string;
		}

		public static IEnumerable<(ReadOnlyMemory<T> memory, bool isLast)> AsIsLast<T>(this ReadOnlySequence<T> seq)
		{
			var enumerator = seq.GetEnumerator();
			if (enumerator.MoveNext() == false)
				yield break;

			var last = enumerator.Current;
			while (enumerator.MoveNext())
			{
				yield return (last, false);
				last = enumerator.Current;
			}

			yield return (last, true);
		}

		public static IEnumerable<(int index, T item)> Enumerate<T>(this IEnumerable<T> enumerable)
		{
			int i = 0;
			foreach (var item in enumerable)
			{
				yield return (i++, item);
			}
		}
	}
}
