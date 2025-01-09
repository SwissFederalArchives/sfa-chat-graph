using Humanizer;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using OpenAI.Chat;
using SfaChatGraph.Server.FunctionCalling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace SfaChatGraph.Server.FunctionCalling
{
	public class CallableFunction
	{
		public ChatTool ChatTool { get; init; }
		public JsonSchema Parameters { get; init; }

		private readonly MethodInfo _method;
		private Type _parameterType;
		private Type _contextType;

		public CallableFunction(MethodInfo info, TypeFactory factory)
		{
			_method = info;
			_parameterType = factory.DefineType($"{info.Name}Parameters", CompileParameterType);
			_contextType = info.DeclaringType;

			Parameters = new JsonSchemaBuilder().Properties().FromType(_parameterType).Build();
			var name = info.Name;
			var description = info.GetCustomAttribute<DescriptionAttribute>()?.Description;
			var parameterData = BinaryData.FromString(Parameters.ToJsonDocument().RootElement.ToString());
			ChatTool = ChatTool.CreateFunctionTool(name, description, parameterData);
		}

		public Task<object> CallAsync(string json, IServiceProvider container)
		{
			var context = container.GetService(_contextType);
			if (context == null)
				throw new ArgumentException($"No service found for type {_contextType.Name}");

			var document = JsonDocument.Parse(json);
			if (Parameters.Evaluate(document).IsValid == false)
				throw new ArgumentException("json not matching the expected schema");

			var parameters = document.Deserialize(_parameterType);
			return ((ICallable)parameters).CallAsync(context);
		}

		private void CompileParameterType(TypeBuilder builder)
		{
			builder.AddInterfaceImplementation(typeof(ICallable));
			var callInfo = typeof(ICallable).GetMethod(nameof(ICallable.CallAsync));
			var method = builder.DefineMethod(callInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(Task<object>), new[] { typeof(object) });
			var methodGen = method.GetILGenerator();
			if (_method.IsStatic == false)
			{
				methodGen.Emit(OpCodes.Ldarg_1);
				methodGen.Emit(OpCodes.Castclass, _method.DeclaringType);
			}

			var requiredAttributeCtor = typeof(RequiredAttribute).GetConstructor(Type.EmptyTypes);
			var requiredAttributeBuilder = new CustomAttributeBuilder(requiredAttributeCtor, new object[0]);

			foreach (var parameter in _method.GetParameters())
			{
				var (field, property) = builder.DefineAutoProperty(parameter.Name.Pascalize(), parameter.ParameterType);
				property.SetCustomAttribute(requiredAttributeBuilder);
				methodGen.Emit(OpCodes.Ldarg_0);
				methodGen.Emit(OpCodes.Ldfld, field);
			}

			methodGen.EmitCall(OpCodes.Callvirt, _method, null);
			var returnType = _method.ReturnType;
			if (returnType.IsGenericType && returnType.GetGenericTypeDefinition().Equals(typeof(Task<>)))
			{
				var genericType = returnType.GetGenericArguments()[0];
				var converMethod = typeof(Extensions)
					.GetMethod(nameof(Extensions.ResultAsObject))
					.MakeGenericMethod(genericType);

				methodGen.EmitCall(OpCodes.Call, converMethod, null);
			}
			else
			{
				methodGen.Emit(OpCodes.Castclass, typeof(object));
				var fromResult = typeof(Task)
					.GetMethod(nameof(Task.FromResult))
					.MakeGenericMethod(typeof(object));

				methodGen.EmitCall(OpCodes.Call, fromResult, null);
			}

			methodGen.Emit(OpCodes.Ret);
			builder.DefineMethodOverride(method, callInfo);
		}



	}
}
