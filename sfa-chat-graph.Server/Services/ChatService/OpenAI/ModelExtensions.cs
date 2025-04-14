using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using SfaChatGraph.Server.Models;
using System.Text.Json;
using VDS.RDF.Query;
using AwosFramework.Generators.FunctionCalling;
using Json.More;

namespace sfa_chat_graph.Server.Services.ChatService.OpenAI
{
	public static class ModelExtensions
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

		public static ApiMessage AsApiMessage(this ChatMessage msg, ApiCodeToolData codeToolData = null)
		{
			return msg switch
			{
				AssistantChatMessage assistantChatMessage => (
					assistantChatMessage.ToolCalls?.Count > 0 ?
					new ApiToolCallMessage(assistantChatMessage.ToolCalls.Select(x => new ApiToolCall(x.FunctionName, x.Id, JsonDocument.Parse(x.FunctionArguments)))) :
					new ApiAssistantMessage(assistantChatMessage.Content.First().Text)
				),
				ToolChatMessage toolMessage => new ApiToolResponseMessage(toolMessage.ToolCallId, toolMessage.Content.First().Text, codeToolData),
				UserChatMessage userMessage => new ApiMessage(ChatRole.User, userMessage.Content.First().Text),
				_ => throw new System.NotImplementedException()
			};
		}

		public static ApiMessage AsApiMessage(this ChatMessage msg, ApiGraphToolData graphToolData = null)
		{
			return msg switch
			{
				AssistantChatMessage assistantChatMessage => (
					assistantChatMessage.ToolCalls?.Count > 0 ?
					new ApiToolCallMessage(assistantChatMessage.ToolCalls.Select(x => new ApiToolCall(x.FunctionName, x.Id, JsonDocument.Parse(x.FunctionArguments)))) :
					new ApiAssistantMessage(assistantChatMessage.Content.First().Text)
				),
				ToolChatMessage toolMessage => new ApiToolResponseMessage(toolMessage.ToolCallId, toolMessage.Content.First().Text,  graphToolData),
				UserChatMessage userMessage => new ApiMessage(ChatRole.User, userMessage.Content.First().Text),
				_ => throw new System.NotImplementedException()
			};
		}
	}
}
