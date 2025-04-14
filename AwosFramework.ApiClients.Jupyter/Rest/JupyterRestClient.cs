using AwosFramework.ApiClients.Jupyter.Rest.Formatters;
using AwosFramework.ApiClients.Jupyter.Rest.Json;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest
{
	public static class JupyterRestClient
	{
		public static IJupyterRestClient GetRestClient(CookieContainer cookies, string endpoint, string? token = null) => GetRestClient(cookies, new Uri(endpoint), token);

		public static IJupyterRestClient GetRestClient(CookieContainer cookies, Uri endpoint, string? token = null)
		{
			if (endpoint.Segments.Last().Equals("api", StringComparison.OrdinalIgnoreCase) == false)
				endpoint = new Uri(endpoint, "api");

			var cookieHandler = new HttpClientHandler()
			{
				CookieContainer = cookies,
				UseCookies = true,
			};

			var client = new HttpClient(cookieHandler, true) { BaseAddress = endpoint };

			if (string.IsNullOrEmpty(token) == false)
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);

			// setup cookies
			client.GetAsync("/tree").Result.EnsureSuccessStatusCode();
			var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
			options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
			options.Converters.Add(new JsonContentModelConverter());
			var refitOptions = new RefitSettings
			{
				ContentSerializer = new SystemTextJsonContentSerializer(options),
				UrlParameterFormatter = new JupyterUrlFormatter(),
			};

			var restClient = RestService.For<IJupyterRestClient>(client, refitOptions);
			options.TypeInfoResolver = new ContentModelTypeInfoResolver(restClient);
			return restClient;
		}
	}

	class DummyMessageHandler : DelegatingHandler
	{
		public DummyMessageHandler()
		{
			InnerHandler = new HttpClientHandler();
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Console.WriteLine($"Request: {request.Method} {request.RequestUri}");
			return base.SendAsync(request, cancellationToken);
		}
	}
}
