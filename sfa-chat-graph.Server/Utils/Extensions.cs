using AwosFramework.Generators.FunctionCalling;
using Json.More;
using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.RDF.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace SfaChatGraph.Server.Utils
{
	public static class Extensions
	{
		public static ChatTool AsChatTool(this IFunctionCallMetadata metadata)
		{
			return ChatTool.CreateFunctionTool(metadata.Id, metadata.Description, BinaryData.FromString(metadata.Schema.ToJsonDocument().RootElement.ToString()));
		}

		public static ChatMessage AsOpenAIMessage(this ApiMessage msg)
		{
			return msg switch
			{
				ApiToolResponseMessage toolMessage => ChatMessage.CreateToolMessage(toolMessage.ToolCallId, toolMessage.Content),
				ApiAssistantMessage assistanceMessage => ChatMessage.CreateAssistantMessage(assistanceMessage.Content),
				ApiToolCallMessage toolCallMessage => ChatMessage.CreateAssistantMessage(toolCallMessage.ToolCalls.Select(x => ChatToolCall.CreateFunctionToolCall(x.ToolCallId, x.ToolId, BinaryData.FromString(x.Arguments.RootElement.ToString())))),
				ApiMessage message => ChatMessage.CreateUserMessage(message.Content),
				_ => throw new System.NotImplementedException()
			};

		}

		public static ApiMessage AsApiMessage(this ChatMessage msg, SparqlStarResult graphResult = null)
		{
			return msg switch
			{
				AssistantChatMessage assistantChatMessage => (
					assistantChatMessage.ToolCalls?.Count > 0 ?
					new ApiToolCallMessage(assistantChatMessage.ToolCalls.Select(x => new ApiToolCall(x.Id, x.FunctionName, JsonDocument.Parse(x.FunctionArguments)))) :
					new ApiAssistantMessage(assistantChatMessage.Content.First().Text)
				),
				ToolChatMessage toolMessage => new ApiToolResponseMessage(toolMessage.ToolCallId, toolMessage.Content.First().Text, graphResult),
				UserChatMessage userMessage => new ApiMessage(ChatRole.User, userMessage.Content.First().Text),
				_ => throw new System.NotImplementedException()
			};
		}

		public static IEnumerable<(int index, T item)> Enumerate<T>(this IEnumerable<T> enumerable)
		{
			int i = 0;
			foreach (var item in enumerable)
			{
				yield return (i++, item);
			}
		}
	}
}
