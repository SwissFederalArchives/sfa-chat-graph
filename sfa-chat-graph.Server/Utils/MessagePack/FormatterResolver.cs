using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System.Text.Json;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.Utils.MessagePack
{
	public class FormatterResolver : IFormatterResolver
	{
		public static readonly IFormatterResolver Instance = new FormatterResolver();
		private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
		{
			new InternalResolver(),
			StandardResolver.Instance
		};

		private static IFormatterResolver Resolver = CompositeResolver.Create(Resolvers);

		public IMessagePackFormatter<T> GetFormatter<T>() => Resolver.GetFormatter<T>();

		private sealed class InternalResolver : IFormatterResolver
		{
			private static readonly JsonDocumentFormatter JsonDocumentFormatter = new JsonDocumentFormatter();
			private static readonly SparqlResultSetFormatter SparqlResultSetFormatter = new SparqlResultSetFormatter();

			public IMessagePackFormatter<T> GetFormatter<T>()
			{
				if (typeof(T) == typeof(JsonDocument))
					return (IMessagePackFormatter<T>)JsonDocumentFormatter;
			
				if(typeof(T) == typeof(SparqlResultSet))
					return (IMessagePackFormatter<T>)SparqlResultSetFormatter;

				return null;
			}
		}
	}
}
