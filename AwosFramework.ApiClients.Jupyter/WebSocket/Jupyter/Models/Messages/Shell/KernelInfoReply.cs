using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell
{
	[MessageType("kernel_info_reply", ChannelKind.Shell)]
	public class KernelInfoReply : ReplyMessage
	{
		[JsonPropertyName("protocol_version")]
		public required string ProtocolVersion { get; set; }

		[JsonPropertyName("implementation")]
		public required string Implementation { get; set; }

		[JsonPropertyName("implementation_version")]
		public required string ImplementationVersion { get; set; }

		[JsonPropertyName("language_info")]
		public required LanguageInfo LanguageInfo { get; set; }

		[JsonPropertyName("banner")]
		public string? Banner { get; set; }

		[JsonPropertyName("debugger")]
		public bool Debbuger { get; set; }

		[JsonPropertyName("help_links")]
		public HelpLink[]? HelpLinks { get; set; }
	}
}
