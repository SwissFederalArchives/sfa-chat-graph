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
		public string? Description { get; }
		public bool IsContextParameter { get; }

		public FunctionCallParameter(int index, string name, string type, string? description, bool isContextParameter)
		{
			this.Index = index;
			this.Name = name;
			this.Type = type;
			this.Description = description;
			this.IsContextParameter = isContextParameter;
		}
	}
}
