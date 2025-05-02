using MessagePack;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Models
{
	[MessagePackObject]
	public class ApiToolResponseMessage : ApiMessage
	{
		[Key(5)]
		public string ToolCallId { get; set; }

		[Key(6)]
		public ApiGraphToolData GraphToolData { get; set; }

		[Key(7)]
		public ApiCodeToolData CodeToolData { get; set; }

		public ApiToolResponseMessage() : base(ChatRole.ToolResponse, null)
		{

		}

		[JsonIgnore]
		[BsonIgnore]
		[IgnoreMember]
		public bool HasData => this.GraphToolData != null || this.CodeToolData != null;

		public ApiToolResponseMessage(string id, string content) : base(ChatRole.ToolResponse, content)
		{
			this.ToolCallId = id;
		}

		public ApiToolResponseMessage(string id, string content, ApiGraphToolData graphData) : base(ChatRole.ToolResponse, content)
		{
			this.ToolCallId = id;
			this.GraphToolData = graphData;
		}

		public ApiToolResponseMessage(string id, string content, ApiCodeToolData codeData) : base(ChatRole.ToolResponse, content)
		{
			this.ToolCallId = id;
			this.CodeToolData = codeData;
		}

	}
}
