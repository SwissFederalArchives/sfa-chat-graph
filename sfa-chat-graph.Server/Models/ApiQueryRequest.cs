namespace SfaChatGraph.Server.Models
{
	public class ApiQueryRequest
	{
		public string Query { get; set; }
		public int MaxErrors { get; set; }
		public int Depth { get; set; }
	}
}
