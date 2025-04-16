using MessagePack;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.RDF.Models;
using System.Text.Json.Serialization;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Models
{
	[MessagePackObject]
	public class ApiToolResponseMessage : ApiMessage
	{
		[Key(4)]
		public string ToolCallId { get; set; }

		[Key(5)]
		public ApiGraphToolData GraphToolData { get; set; }

		[Key(6)]
		public ApiCodeToolData CodeToolData { get; set; }

		public ApiToolResponseMessage() : base(ChatRole.ToolResponse, null)
		{

		}

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
