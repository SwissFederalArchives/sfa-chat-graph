using AwosFramework.Generators.FunctionCalling.Utils;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AwosFramework.Generators.FunctionCalling
{
	public readonly record struct FunctionCall
	{
		public string FunctionId { get; }
		public string Description { get; }
		public string ContainingType { get; }
		public string MethodName { get; }
		public string ModelClassName => string.Format(Constants.ModelClassNameFormat, FunctionId.ToCamelCase());
		public bool IsStatic { get; }
		public bool IsTask { get; }
		public EquatableArray<FunctionCallParameter> Parameters { get; }

		public FunctionCall(string functionId, string description, string containingType, string methodName, bool isStatic, bool isTask, FunctionCallParameter[] parameters)
		{
			this.FunctionId = functionId;
			this.Description = description;
			this.ContainingType = containingType;
			this.MethodName = methodName;
			this.IsStatic = isStatic;
			this.IsTask = isTask;
			this.Parameters = new EquatableArray<FunctionCallParameter>(parameters);
		}
	}
}
