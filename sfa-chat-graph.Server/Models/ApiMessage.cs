using MessagePack;
using MongoDB.Bson.Serialization.Attributes;
using OpenAI.Assistants;
using SfaChatGraph.Server.Models;
using System.Text.Json.Serialization;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Models
{
	[MessagePackObject]
	[Union(1, typeof(ApiAssistantMessage))]
	[Union(2, typeof(ApiToolCallMessage))]
	[Union(3, typeof(ApiToolResponseMessage))]
	[BsonKnownTypes(typeof(ApiAssistantMessage), typeof(ApiToolCallMessage), typeof(ApiToolResponseMessage))]
	public class ApiMessage
	{
		[Key(0)]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Key(1)]
		public ChatRole Role { get; set; }

		[Key(2)]
		public string Content { get; set; }

		[Key(3)]
		public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

		public ApiMessage() : this(ChatRole.User, null)
		{

		}

		public ApiMessage(ChatRole role, string content)
		{
			this.Role = role;
			this.Content = content;
		}

		public static ApiMessage UserMessage(string content) => new ApiMessage(ChatRole.User, content);
		public static ApiToolResponseMessage ToolResponse(string id, string content) => new ApiToolResponseMessage(id, content);

	}
}
