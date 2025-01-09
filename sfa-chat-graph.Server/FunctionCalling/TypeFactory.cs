using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SfaChatGraph.Server.FunctionCalling
{

	public class TypeFactory
	{
		public AssemblyName AssemblyName { get; init; }
		private AssemblyBuilder _assemblyBuilder;
		private ModuleBuilder _moduleBuilder;
		private readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();

		public TypeFactory(string assemblyName)
		{
			AssemblyName = new AssemblyName(assemblyName);
			_assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.RunAndCollect);
			_moduleBuilder = _assemblyBuilder.DefineDynamicModule(assemblyName);
		}


		public Type DefineType(string name, Action<TypeBuilder> builderAction)
		{
			if (_typeCache.ContainsKey(name))
				return _typeCache[name];

			var builder = _moduleBuilder.DefineType(name);
			builderAction?.Invoke(builder);
			var type = builder.CreateType();
			_typeCache.Add(name, type);
			return type;
		}
	}
}
