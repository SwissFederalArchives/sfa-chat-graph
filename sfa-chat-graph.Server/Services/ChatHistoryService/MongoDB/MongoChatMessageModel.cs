using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using sfa_chat_graph.Server.Models;
using SfaChatGraph.Server.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace sfa_chat_graph.Server.Services.ChatHistoryService.MongoDB
{
	public class MongoChatMessageModel
	{
		[BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid HistoryId { get; set; }

		[BsonId]
		[BsonElement("_id")]
		[BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid MessageId { get; set; }

		public ChatRole Role { get; set; }
		public string Content { get; set; }
		public DateTime TimeStamp { get; set; }
		public ApiToolCall[] ToolCalls { get; set; }
		public string ToolCallId { get; set; }
		public bool HasGraphData { get; set; }
		public bool HasCodeData { get; set; }

		[BsonIgnore]
		public ApiGraphToolData GraphToolData { get; set; }

		[BsonIgnore]
		public ApiCodeToolData CodeToolData { get; set; }

		[BsonIgnore]
		public bool HasData => Role == ChatRole.ToolResponse && (HasGraphData || HasCodeData);

		public MongoChatMessageModel()
		{

		}

		public MongoChatMessageModel(Guid historyId, Guid id, ChatRole role, string content, DateTime timeStamp, ApiToolCall[] toolCalls, string toolCallId, ApiGraphToolData graphToolData, ApiCodeToolData codeToolData)
		{
			HistoryId=historyId;
			MessageId=id;
			Role=role;
			Content=content;
			TimeStamp=timeStamp;
			ToolCalls=toolCalls;
			ToolCallId=toolCallId;
			GraphToolData=graphToolData;
			CodeToolData=codeToolData;
			HasGraphData = graphToolData != null;
			HasCodeData = codeToolData != null;
		}

		private ApiMessage ToApiMessage() => new ApiMessage(Role, Content) { TimeStamp = TimeStamp, Id = MessageId };
		private ApiToolCallMessage ToApiToolCallMessage() => new ApiToolCallMessage(ToolCalls) { TimeStamp = TimeStamp, Id = MessageId };
		private ApiToolResponseMessage ToApiToolResponseMessage() => new ApiToolResponseMessage(ToolCallId, Content) { TimeStamp = TimeStamp, Id = MessageId, GraphToolData = GraphToolData, CodeToolData = CodeToolData };

		public ApiMessage ToApi => Role switch
		{
			ChatRole.User => ToApiMessage(),
			ChatRole.Assistant => ToApiMessage(),
			ChatRole.ToolCall => ToApiToolCallMessage(),
			ChatRole.ToolResponse => ToApiToolResponseMessage(),
			_ => throw new InvalidOperationException($"Unknown message role {Role}")
		};

		private static MongoChatMessageModel FromApiMessage(Guid historyId, ApiMessage message) => new MongoChatMessageModel(historyId, message.Id, message.Role, message.Content, message.TimeStamp, null, null, null, null);
		private static MongoChatMessageModel FromApiToolCallMessage(Guid historyId, ApiToolCallMessage message) => new MongoChatMessageModel(historyId, message.Id, message.Role, message.Content, message.TimeStamp, message.ToolCalls, null, null, null);
		private static MongoChatMessageModel FromApiToolResponseMessage(Guid historyId, ApiToolResponseMessage message) => new MongoChatMessageModel(historyId, message.Id, message.Role, message.Content, message.TimeStamp, null, message.ToolCallId, message.GraphToolData, message.CodeToolData);

		public static MongoChatMessageModel FromApi(Guid historyId, ApiMessage message) => message switch
		{
			ApiToolResponseMessage toolResponseMessage => FromApiToolResponseMessage(historyId, toolResponseMessage),
			ApiAssistantMessage assistantMessage => FromApiMessage(historyId, assistantMessage),
			ApiToolCallMessage apiToolCallMessage => FromApiToolCallMessage(historyId, apiToolCallMessage),
			ApiMessage userMessage => FromApiMessage(historyId, userMessage),
			_ => throw new NotImplementedException()
		};
	}
}
