using AwosFramework.Generators.FunctionCalling.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace AwosFramework.Generators.FunctionCalling
{
	[Generator]
	public class FunctionCallGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			context.RegisterPostInitializationOutput(ctx =>
			{
				ctx.AddSource($"{Constants.MarkerAttributeName}.g.cs", SourceText.From(Constants.MarkerAttribute, Encoding.UTF8));
				ctx.AddSource($"{Constants.CallableInterfaceName}.g.cs", SourceText.From(Constants.CallableInterface, Encoding.UTF8));
				ctx.AddSource($"{Constants.ParentResolverInterfaceName}.g.cs", SourceText.From(Constants.ParentResolverInterface, Encoding.UTF8));
				ctx.AddSource($"{Constants.FunctionCallMetadataInterfaceName}.g.cs", SourceText.From(Constants.FunctionCallMetadataInterface, Encoding.UTF8));
				ctx.AddSource($"{Constants.FunctionCallMetadataName}.g.cs", SourceText.From(Constants.FunctionCallMetadata, Encoding.UTF8));
				ctx.AddSource($"{Constants.ExtensionsClassName}.g.cs", SourceText.From(Constants.ExtensionsClass, Encoding.UTF8));
				ctx.AddSource($"{Constants.ServiceProviderParentResolverName}.g.cs", SourceText.From(Constants.ServiceProviderParentResolver, Encoding.UTF8));
			});

			var functionCallsToGenerate = context.SyntaxProvider
				.ForAttributeWithMetadataName(
					Constants.MarkerAttributeFullName,
					predicate: IsGenerationTarget,
					transform: GetFunctionCallInfo
				).Where(static m => m != null);


			context.RegisterSourceOutput(functionCallsToGenerate, GenerateFunctionCallCode);
			context.RegisterSourceOutput(functionCallsToGenerate.Collect(), GenerateFunctionCallRegistry);
		}

		public static bool IsGenerationTarget(SyntaxNode node, CancellationToken cancelToken)
		{
			return node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0;
		}

		public static FunctionCall? GetFunctionCallInfo(GeneratorAttributeSyntaxContext context, CancellationToken token)
		{
			if (context.Attributes.Length != 1)
				return null;

			var attrib = context.Attributes.First();
			var method = (MethodDeclarationSyntax)context.TargetNode;

			var functionCallId = attrib.ConstructorArguments.First();
			if (functionCallId.IsNull)
				return null;

			var descriptionAttrib = method.AttributeLists
				.SelectMany(x => x.Attributes)
				.FirstOrDefault(x => x.ArgumentList != null && x.ArgumentList.Arguments.Count == 1 && context.SemanticModel.GetSymbolInfo(x).Symbol.ToDisplayString().Equals("System.ComponentModel.DescriptionAttribute.DescriptionAttribute(string)"));

			var description = descriptionAttrib == null ? string.Empty : (string)context.SemanticModel.GetConstantValue(descriptionAttrib.ArgumentList.Arguments.First().Expression).Value;

			var id = (string)functionCallId.Value;
			var containingType = context.SemanticModel.GetEnclosingSymbol(method.SpanStart).ToDisplayString();
			var isStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword);
			var parameters = method.ParameterList.Parameters.Select((s, i) => GetParameter(context, i, s));
			var methodNode = method.Identifier.Text;
			var isTask = method.ReturnType is GenericNameSyntax g && g.Identifier.Text == "Task";
			return new FunctionCall(id, description, containingType, methodNode, isStatic, isTask, parameters.ToArray());
		}

		public static FunctionCallParameter GetParameter(GeneratorAttributeSyntaxContext context, int index, ParameterSyntax syntax)
		{
			var name = syntax.Identifier.Text.ToCamelCase();
			var descriptionAttribute = syntax.AttributeLists.SelectMany(x => x.Attributes)
				.FirstOrDefault(x => x.ArgumentList != null && 
					x.ArgumentList.Arguments.Count == 1 && 
					context.SemanticModel.GetSymbolInfo(x).Symbol.ToDisplayString().Equals("System.ComponentModel.DescriptionAttribute.DescriptionAttribute(string)")
				);
			var description = descriptionAttribute?.ArgumentList.Arguments.Select(x => x.Expression).OfType<LiteralExpressionSyntax>().FirstOrDefault()?.Token.ValueText;
			var type = context.SemanticModel.GetSymbolInfo(syntax.Type).Symbol.ToDisplayString();
			return new FunctionCallParameter(index, name, type, description);
		}

		public static void GenerateFunctionCallRegistry(SourceProductionContext context, ImmutableArray<FunctionCall?> maybeFunctionCalls)
		{
			
			var handlerDictValues = string.Join(",\r\n", maybeFunctionCalls
				.NotNull()
				.Select(x => $"\t\t\t\t{{\"{x.FunctionId}\", new {Constants.FunctionCallMetadataName}(\"{x.FunctionId}\", \"{x.Description.ToLiteral()}\", {x.ModelClassName}.{Constants.SchemaFieldName}, {x.ModelClassName}.{Constants.ResolveAndHandleFunctionName})}}"));

			var constFunctionIds = string.Join("\r\n", maybeFunctionCalls
				.NotNull()
				.Select(x => $"\t\tpublic const string {x.FunctionId.ToUpper()} = \"{x.FunctionId}\";"));

			var registryClass = $$"""
			using System.Collections.Frozen;
			using System.Collections.Generic;
			using System.Text.Json;
			using {{Constants.ModelNameSpace}};

			namespace {{Constants.NameSpace}}
			{
				public class {{Constants.RegistryClassName}}
				{
					
			{{constFunctionIds}}

					private static readonly FrozenDictionary<string, {{Constants.FunctionCallMetadataName}}> {{Constants.RegistryHandlerDictFieldName}};
					private readonly IParentResolver {{Constants.RegistryResolverFieldName}};

					static {{Constants.RegistryClassName}}()
					{
						{{Constants.RegistryHandlerDictFieldName}} = new Dictionary<string, {{Constants.FunctionCallMetadataName}}>
						{
			{{handlerDictValues}}
						}.ToFrozenDictionary();
					}

					public {{Constants.RegistryClassName}}({{Constants.ParentResolverInterfaceName}} resolver)
					{
						{{Constants.RegistryResolverFieldName}} = resolver;
					}
			
					public Task<object> CallFunctionAsync(string id, JsonDocument parameters)
					{
						if({{Constants.RegistryHandlerDictFieldName}}.TryGetValue(id, out var handler))
						{
							return handler.Handler(parameters, {{Constants.RegistryResolverFieldName}});
						}
						else
						{
							throw new InvalidOperationException("Function not found");
						}
					}

					public IEnumerable<{{Constants.FunctionCallMetadataInterfaceName}}> GetFunctionCallMetas()
					{
						return {{Constants.RegistryHandlerDictFieldName}}.Values;
					}

				}
			}
			""";

			context.AddSource($"{Constants.RegistryClassName}.g.cs", SourceText.From(registryClass, Encoding.UTF8));
		}


		private static string FormatParameter(FunctionCallParameter parameter)
		{
			var paramString = $"\t\tpublic {parameter.Type} {parameter.Name} {{ get; set; }}";
			if (string.IsNullOrEmpty(parameter.Description) == false)
				paramString = $"\t\t[System.ComponentModel.Description(\"{parameter.Description}\")]\r\n{paramString}";

			return paramString;
		}


		public static void GenerateFunctionCallCode(SourceProductionContext context, FunctionCall? maybeFunctionCall)
		{
			if (maybeFunctionCall.HasValue == false)
				return;

			var functionCall = maybeFunctionCall.Value;

			var parameters = string.Join("\r\n", functionCall.Parameters.Select(FormatParameter));

			var modelClassName = functionCall.ModelClassName;
			var methodInvoke = $"{functionCall.MethodName}({string.Join(", ", functionCall.Parameters.OrderBy(x => x.Index).Select(x => $"this.{x.Name}"))})";

			var parentName = functionCall.IsStatic ? functionCall.ContainingType : "Parent";
			var invokeFunction =
			$$"""
					public async Task<object> InvokeAsync()
					{
						return{{(functionCall.IsTask ? " await" : "")}} {{parentName}}.{{methodInvoke}};
					}
			""";

			var resolveAndHandleFunction = 
			$$"""
					public static Task<object> {{Constants.ResolveAndHandleFunctionName}}(JsonDocument document, IParentResolver parentResolver)
					{
						if (TryParse(document, out var model))
						{
							{{(functionCall.IsStatic ? "" : $"model.Parent = parentResolver.ResolveParent<{functionCall.ContainingType}>();")}}
							return model.InvokeAsync();
						}
						else
						{
							throw new InvalidOperationException("Invalid function call");
						}
					}
			""";

			var parseFunction =
			$$"""
					public static bool TryParse(JsonDocument document, out {{modelClassName}} model)
					{
						if ({{Constants.SchemaFieldName}}.Evaluate(document).IsValid == false)
						{
							model = null;
							return false;
						}
						else
						{
							model = JsonSerializer.Deserialize<{{modelClassName}}>(document);
							return true;
						}
					}	
			""";

			var parentProperty = functionCall.IsStatic ? "" :
			$$"""
					[JsonIgnore]
					public {{functionCall.ContainingType}} Parent { get; private set; }
			""";

			var modelClass = $$"""
			using Humanizer;
			using Json.More;
			using Json.Schema;
			using Json.Schema.Generation;
			using System.Text.Json;
			using System.Text.Json.Serialization;
			using System.ComponentModel;
			using Json.Schema.Generation.DataAnnotations;
						
			namespace {{Constants.ModelNameSpace}}
			{
				public class {{modelClassName}} : {{Constants.CallableInterfaceName}}
				{
					internal static readonly JsonSchema {{Constants.SchemaFieldName}};


					static {{modelClassName}}() 
					{
						DataAnnotationsSupport.AddDataAnnotations();
						{{Constants.SchemaFieldName}} = new JsonSchemaBuilder().Properties().FromType(typeof({{modelClassName}})).Build();
					}

			{{parseFunction}}

			{{resolveAndHandleFunction}}

			{{parentProperty}}

			{{parameters}}

			{{invokeFunction}} 
				}
			}
			""";

			context.AddSource($"Models/{modelClassName}.g.cs", SourceText.From(modelClass, Encoding.UTF8));

		}
	}
}
