using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.FunctionCalling
{
	public readonly record struct FunctionCallParameter
	{
		public string Name { get; }
		public string Type { get; }
		public int Index { get; }

		public FunctionCallParameter(int index, string name, string type)
		{
			this.Index = index;
			this.Name = name;
			this.Type = type;
		}
	}
}
