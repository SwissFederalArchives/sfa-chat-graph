﻿using AwosFramework.Generators.FunctionCalling;
using Json.Schema.Generation.Intents;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.Services.CodeExecutionService;
using SfaChatGraph.Server.Utils;
using System.ComponentModel;

namespace SfaChatGraph.Server.Services.ChatService
{
	public class ChatCodeService
	{
		private readonly ICodeExecutionService _executionService;

		public ChatCodeService(ICodeExecutionService code)
		{
			_executionService=code;
		}

		[FunctionCall("execute_code")]
		public async Task<CodeExecutionResult> ExecuteCodeAsync([Description("The code to execute")] string code, [Description("A list of id's of previous tool responses. The data of those responses will be available as csv file to the code. The file will be named <tool_reponse_id>.csv. The csv values are separated by ';'.")] string[] toolData, [Context] ChatContext chatContext)
		{
			var additionalFiles = chatContext.ToolResponses.Where(x => toolData?.Contains(x.ToolCallId) == true && x.GraphToolData != null).ToArray();
			if (toolData != null && toolData.Length > additionalFiles.Length)
			{
				var missing = toolData.Where(x => additionalFiles.Any(y => y.ToolCallId == x) == false).ToArray();
				return new CodeExecutionResult { Success = false, Error = $"Not all tool data was found. Following calls are missing or have no data {string.Join(",", missing)}" };
			}

			var files = additionalFiles.Select(x => new CodeExecutionData {  IsBinary = false, Data = LLMFormatter.ToCSV(x.GraphToolData.DataGraph), Name = $"{x.ToolCallId}.csv" }).ToArray();
			return await _executionService.ExecuteCodeAsync(code, files, CancellationToken.None, chatContext.NotifyActivityAsync);
		}
	}
}
