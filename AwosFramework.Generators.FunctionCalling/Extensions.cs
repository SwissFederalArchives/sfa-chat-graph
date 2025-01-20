using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.FunctionCalling
{
	public static class Extensions
	{
		public static string Capitalize(this string str)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			return char.ToUpperInvariant(str[0]) + str.Substring(1);
		}

		public static string ToLiteral(this string @string) => SymbolDisplay.FormatLiteral(@string, false);

		private static char[] SplitSymbols = "_-".ToCharArray();

		public static string ToCamelCase(this string str)
		{
			return string.Join("", str.Split(SplitSymbols).Select(x => x.Capitalize())).Capitalize();
		}
	}
}
