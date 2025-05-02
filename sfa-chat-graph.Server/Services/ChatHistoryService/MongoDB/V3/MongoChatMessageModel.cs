using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using sfa_chat_graph.Server.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace sfa_chat_graph.Server.Services.ChatHistoryService.MongoDB.V3
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
		public int Index { get; set; }

		public MongoGraphToolData GraphToolData { get; set; }
		public MongoCodeToolData CodeToolData { get; set; }


		[BsonIgnore]
		public bool HasData => GraphToolData != null || CodeToolData != null;

		public MongoChatMessageModel()
		{

		}

		public MongoChatMessageModel(Guid historyId, Guid id, ChatRole role, string content, DateTime timeStamp, int index, ApiToolCall[] toolCalls, string toolCallId, ApiGraphToolData graphToolData, ApiCodeToolData codeToolData)
		{
			HistoryId=historyId;
			MessageId=id;
			Role=role;
			Index=index;
			Content=content;
			TimeStamp=timeStamp;
			ToolCalls=toolCalls;
			ToolCallId=toolCallId;
			GraphToolData=MongoGraphToolData.FromApi(graphToolData);
			CodeToolData=MongoCodeToolData.FromApi(codeToolData);
		}

		private ApiMessage ToApiMessage() => new ApiMessage(Role, Content) {  Index = this.Index, TimeStamp = this.TimeStamp, Id = MessageId };
		private ApiToolCallMessage ToApiToolCallMessage() => new ApiToolCallMessage(ToolCalls) { Index = this.Index, TimeStamp = this.TimeStamp, Id = MessageId };
		private ApiToolResponseMessage ToApiToolResponseMessage() => new ApiToolResponseMessage(ToolCallId, Content) { Index = this.Index, TimeStamp = this.TimeStamp, Id = MessageId, CodeToolData = CodeToolData?.ToApi(), GraphToolData = GraphToolData?.ToApi() };

		public ApiMessage ToApi() => Role switch
		{
			ChatRole.User => ToApiMessage(),
			ChatRole.Assistant => ToApiMessage(),
			ChatRole.ToolCall => ToApiToolCallMessage(),
			ChatRole.ToolResponse => ToApiToolResponseMessage(),
			_ => throw new InvalidOperationException($"Unknown message role {Role}")
		};

		private static MongoChatMessageModel FromApiMessage(Guid historyId, ApiMessage message) => new MongoChatMessageModel(historyId, message.Id, message.Role, message.Content, message.TimeStamp, message.Index, null, null, null, null);
		private static MongoChatMessageModel FromApiToolCallMessage(Guid historyId, ApiToolCallMessage message) => new MongoChatMessageModel(historyId, message.Id, message.Role, message.Content, message.TimeStamp, message.Index, message.ToolCalls, null, null, null);
		private static MongoChatMessageModel FromApiToolResponseMessage(Guid historyId, ApiToolResponseMessage message) => new MongoChatMessageModel(historyId, message.Id, message.Role, message.Content, message.TimeStamp, message.Index, null, message.ToolCallId, message.GraphToolData, message.CodeToolData);

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
