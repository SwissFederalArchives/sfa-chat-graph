using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SfaChatGraph.Server.FunctionCalling
{
	public static class Extensions
	{
		public static (FieldBuilder backingField, PropertyBuilder property) DefineAutoProperty(this TypeBuilder builder, string propertyName, Type propertyType, bool hasPrivateSetter = false)
		{
			var fieldBuilder = builder.DefineField($"<{propertyName}>k__BackingField", propertyType, FieldAttributes.Private);
			var propertyBuilder = builder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);

			var getAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
			var getterBuilder = builder.DefineMethod($"get_{propertyName}", getAttr, propertyType, Type.EmptyTypes);
			var getterGen = getterBuilder.GetILGenerator();
			getterGen.Emit(OpCodes.Ldarg_0);
			getterGen.Emit(OpCodes.Ldfld, fieldBuilder);
			getterGen.Emit(OpCodes.Ret);
			propertyBuilder.SetGetMethod(getterBuilder);

			var setAttr = (hasPrivateSetter ? MethodAttributes.Private : MethodAttributes.Public) | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
			var setterBuilder = builder.DefineMethod($"set_{propertyName}", setAttr, null, new Type[] { propertyType });
			var setterGen = setterBuilder.GetILGenerator();
			setterGen.Emit(OpCodes.Ldarg_0);
			setterGen.Emit(OpCodes.Ldarg_1);
			setterGen.Emit(OpCodes.Stfld, fieldBuilder);
			setterGen.Emit(OpCodes.Ret);
			propertyBuilder.SetSetMethod(setterBuilder);
			return (fieldBuilder, propertyBuilder);
		}


		public static CallableFunction GetCallableFunction(this Type type, string methodName, TypeFactory factory)
		{
			var method = type.GetMethod(methodName);
			if (method == null)
				throw new ArgumentException($"Type {type.Name} does not contain a method {methodName}");

			return new CallableFunction(method, factory);
		}


		public static CallableFunction[] GetCallableFunctions(this Type type, TypeFactory factory, Func<MethodInfo, bool> filter = null)
		{
			var methods = type.GetMethods();
			if (filter != null)
				methods = methods.Where(filter).ToArray();

			var funcs = new List<CallableFunction>();
			foreach (var method in methods)
				funcs.Add(new CallableFunction(method, factory));

			return funcs.ToArray();
		}

		public static async Task<object> ResultAsObject<T>(this Task<T> task)
		{
			object res = await task;
			return res;
		}

		public static string SqlEscape(this string @string)
		{
			return string.Join(".", @string.Split(".").Select(x => x.StartsWith("[") && x.EndsWith("]") ? x : $"[{x}]"));
		}
	}
}
