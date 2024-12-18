using Microsoft.AspNetCore.Mvc;
using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server
{
	[ApiController]
	[Route("/api/v1/rdf")]
	public class RdfController : ControllerBase
	{

		[HttpPost("query")]
		public async Task<IActionResult> QueryAsync([FromBody]ApiQueryRequest request)
		{

		}
	}
}
