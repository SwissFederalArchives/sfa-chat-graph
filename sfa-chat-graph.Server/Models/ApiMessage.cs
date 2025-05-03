using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OpenAI.Assistants;
using System.Text.Json.Serialization;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.Models
{
	[MessagePackObject]
	[BsonKnownTypes(typeof(ApiAssistantMessage), typeof(ApiToolCallMessage), typeof(ApiToolResponseMessage))]
	public class ApiMessage : IApiMessage
	{
		[Key(0)]
		[BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Key(1)]
		public ChatRole Role { get; set; }

		[Key(2)]
		public string Content { get; set; }

		[Key(3)]
		public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

		[Key(4)]
		public int Index { get; set; }

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
