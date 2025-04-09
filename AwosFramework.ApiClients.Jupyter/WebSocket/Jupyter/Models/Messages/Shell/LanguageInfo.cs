using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell
{
	public class LanguageInfo
	{
		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("version")]
		public required string Version { get; set; }

		[JsonPropertyName("mimetype")]
		public required string MimeType { get; set; }

		[JsonPropertyName("file_extension")]
		public required string FileExtension { get; set; }

		[JsonPropertyName("pygments_lexer")]
		public string? PygmentsLexer { get; set; }

		[JsonPropertyName("codemirror_mode")]
		public string? CodemirrorMode { get; set; }

		[JsonPropertyName("nbconvert_exporter")]
		public string? NbConvertExporter { get; set; }
	}
}
