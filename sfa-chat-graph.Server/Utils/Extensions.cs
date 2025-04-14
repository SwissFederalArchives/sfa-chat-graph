using AwosFramework.Generators.FunctionCalling;
using Json.More;
using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.RDF.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.Utils
{
	public static class Extensions
	{
		public static IEnumerable<TResult> SelectWhere<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> mapper, Func<TSource, bool> predicate) => enumerable.Where(predicate).Select(mapper);
		public static IEnumerable<TResult> SelectNonNull<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> mapper) => enumerable.SelectWhere(mapper, x => x!= null);
		
		public static string ToIriList(this IEnumerable<string> iris) => string.Join(" ", iris.Select(x => $"<{x}>"));

		public static void AddRange<T>(this ICollection<T> ts, IEnumerable<T> items)
		{
			foreach (var item in items)
				ts.Add(item);
		}

		public static string Ellipsis(this string @string, int maxLen, string end = "...")
		{
			var len = maxLen - end.Length;
			if(@string.Length > len)
				return @string.Substring(0, len) + end;

			return @string;
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
