using VDS.RDF;

namespace SfaChatGraph.Server.RDF
{
	public class RelativeCachingUriFactory : IUriFactory
	{
		private readonly CachingUriFactory _factory;
		private readonly Uri _baseUri;

		public RelativeCachingUriFactory(CachingUriFactory factory, Uri baseUri)
		{
			_factory=factory;
			_baseUri=baseUri;
		}

		public bool InternUris {
			get => _factory.InternUris;
			set => _factory.InternUris = value;
		}

		public void Clear()
		{
			_factory.Clear();
		}

		public Uri Create(string uriString)
		{
			if(Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri))
			{
				if (uri.IsAbsoluteUri)
				{
					return _factory.Create(uri.AbsoluteUri);
				}
				else
				{
					return Create(_baseUri, uriString);
				}
			}
			else
			{
				throw new UriFormatException($"Invalid URI: {uri}");
			}
		}

		public Uri Create(Uri baseUri, string relativeUri) => _factory.Create(baseUri, relativeUri);

		public bool TryGetUri(string uri, out Uri value) => _factory.TryGetUri(uri, out value);
	}
}
