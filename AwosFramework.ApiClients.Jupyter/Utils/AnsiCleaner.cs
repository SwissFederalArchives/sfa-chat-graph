using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Utils
{
	using System;
	using System.Text;
	using System.Text.RegularExpressions;

	public static class AnsiCleaner
	{
		public static string? CleanAnsiString(string? input)
		{
			if (input == null)
				return null;

			// Regex to match ANSI escape sequences (CSI, OSC, etc.)
			string ansiPattern = @"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])";
			string noAnsi = Regex.Replace(input, ansiPattern, "");

			// Handle backspaces
			var output = new StringBuilder();
			foreach (char c in noAnsi)
			{
				if (c == '\b')
				{
					if (output.Length > 0)
						output.Length--; // Remove last character
				}
				else
				{
					output.Append(c);
				}
			}

			return output.ToString();
		}

	}

}
