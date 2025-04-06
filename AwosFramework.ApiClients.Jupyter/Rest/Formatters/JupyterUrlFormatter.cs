using AwosFramework.ApiClients.Jupyter.Rest.Models;
using AwosFramework.ApiClients.Jupyter.Utils;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Formatters
{
	public class JupyterUrlFormatter : IUrlParameterFormatter
	{
		private readonly IUrlParameterFormatter _defaultFormatter;

		public JupyterUrlFormatter() : this(new DefaultUrlParameterFormatter())
		{

		}

		public JupyterUrlFormatter(IUrlParameterFormatter defaultFormatter)
		{
			_defaultFormatter = defaultFormatter;
		}

		private bool IsEnum(Type type)
		{
			if (type.IsEnum)
				return true;

			type = Nullable.GetUnderlyingType(type);
			if (type != null && type.IsEnum)
				return true;

			return false;
		}

		public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
		{
			if (value is bool boolean)
				return boolean ? "1" : "0";

			if (IsEnum(type))
				return value?.ToString()?.ToSnakeCase();

			return _defaultFormatter.Format(value, attributeProvider, type);
		}
	}
}
