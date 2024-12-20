using System.Runtime.CompilerServices;

namespace sfa_chat_graph.Server.Utils
{
	public static class Extensions
	{
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
