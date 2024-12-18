namespace sfa_chat_graph.Server.RDF
{
	public class OntotextRepositoryModel
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public Uri Uri { get; set; }
		public Uri ExternalUri { get; set; }
		public bool Local { get; set; }
		public string Type { get; set; }
		public string SesameType { get; set; }
		public string Location { get; set; }
		public bool Readable { get; set; }
		public bool Writable { get; set; }
		public bool Unsupported { get; set; }
		public string State { get; set; }
	}
}
