namespace SfaChatGraph.Server.Services.CodeExecutionService.Jupyter
{
	public class JupyterCodeExecutionServiceOptions
	{
		public required string Endpoint { get; set; }
		public string? Token { get; set; }
		public string? Kernel { get; set; }
		public string? SetupScript { get; set; }
	}
}
