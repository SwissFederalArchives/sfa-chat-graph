using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Json;
using System.Text.Json.Serialization;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell
{
	[JsonConverter(typeof(HistoryEntryConverter))]
	public class HistoryEntry
	{
		public int Session { get; set; }
		public int LineNumber { get; set; }
		public required string Input { get; set; }
		public string? Output { get; set; }
	}


}
