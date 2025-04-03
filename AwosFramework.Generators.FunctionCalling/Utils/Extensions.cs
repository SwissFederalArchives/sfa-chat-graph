using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.FunctionCalling.Utils
{
	public static class Extensions
	{
		public static IEnumerable<T> NotNull<T>(this IEnumerable<Nullable<T>> source) where T : struct
		{
			foreach(var item in source)
			{
				if (item is not null)
					yield return item.Value;
			}	
		}
	}
}
