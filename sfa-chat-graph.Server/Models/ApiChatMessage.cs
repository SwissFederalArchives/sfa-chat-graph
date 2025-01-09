using Json.More;
using OpenAI.Assistants;
using OpenAI.Chat;
using sfa_chat_graph.Server.Utils;
using SfaChatGraph.Server.RDF.Models;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;

namespace SfaChatGraph.Server.Models
{
	public class ApiChatMessage
	{
		public string Content { get; set; }
		public string ToolCallId { get; set; }
		public JsonDocument OriginalData { get; set; }
		public SparqlStarResult Graph { get; set; }
		public ChatRole Role { get; set; }

		public static ApiChatMessage FromChatMessage(AssistantChatMessage message)
		{
			if (message.ToolCalls.Count == 0)
			{
				return new ApiChatMessage
				{
					Role = ChatRole.Assistant,
					Content = message.Content.First().Text,
					OriginalData = JsonSerializer.SerializeToDocument(message)
				};
			}
			else
			{
				return new ApiChatMessage
				{
					Role = ChatRole.ToolCall,
					Content = JsonSerializer.Serialize(message.ToolCalls.First()),
					ToolCallId = message.ToolCalls.First().Id,
					OriginalData = JsonSerializer.SerializeToDocument(message)
				};
			}
		}

		public static ApiChatMessage FromChatMessage(UserChatMessage message)
		{
			return new ApiChatMessage
			{
				Role = ChatRole.User,
				Content = message.Content.First().Text,
			};
		}

		public static ApiChatMessage FromChatMessage(ToolChatMessage toolMessage, SparqlStarResult graphResult = null)
		{
			return new ApiChatMessage
			{
				Role = ChatRole.Tool,
				Content = toolMessage.Content.First().Text,
				Graph = graphResult
			};
		}

		public ChatMessage ToChatMessage()
		{
			switch (Role)
			{
				case ChatRole.User:
					return ChatMessage.CreateUserMessage(Content);

				case ChatRole.Assistant:
				case ChatRole.ToolCall:
					return PrivateCtorTypeInfoResolver.Deserialize<AssistantChatMessage>(OriginalData);

				case ChatRole.Tool:
					return ToolChatMessage.CreateToolMessage(ToolCallId, Content);

				default:
					throw new InvalidOperationException($"Unknown ChatRole: {Role}");
			}

		}

	}
}
