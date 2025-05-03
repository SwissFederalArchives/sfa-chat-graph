using SfaChatGraph.Server.Models;

namespace SfaChatGraph.Server.Services.ChatHistoryService.MongoDB.V2
{
	public class MongoCodeToolData
	{
		public string Error { get; set; }
		public string Code { get; set; }
		public string Language { get; set; }
		public bool Success { get; set; }
		public MongoToolData[] ToolData { get; set; }

		public ApiCodeToolData ToApi()
		{
			return new ApiCodeToolData
			{
				Error = Error,
				Code = Code,
				Language = Language,
				Success = Success,
				Data = ToolData?.Select(MongoToolData.ToApi)?.ToArray()
			};
		}

		public static MongoCodeToolData FromApi(ApiCodeToolData data)
		{
			if (data == null) return null;
			return new MongoCodeToolData
			{
				Error = data.Error,
				Code = data.Code,
				Language = data.Language,
				Success = data.Success,
				ToolData = data.Data?.Select(MongoToolData.FromApi)?.ToArray()
			};
		}
	}
}
