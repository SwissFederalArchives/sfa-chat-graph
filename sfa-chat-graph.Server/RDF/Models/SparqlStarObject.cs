using System.Collections.Frozen;
using System.Dynamic;

namespace sfa_chat_graph.Server.RDF.Models
{
	public class SparqlStarObject
	{
		private SparqlStarTerm[] _terms { get; set; }
		private FrozenDictionary<string, int> _termMapping { get; set; }

		public SparqlStarTerm this[string key] => _terms[_termMapping[key]];
		public SparqlStarTerm this[int index] => _terms[index];

		public bool ContainsKey(string key) => _termMapping.ContainsKey(key);
		
		public bool TryGetTerm(string key, out SparqlStarTerm term)
		{
			if (_termMapping.TryGetValue(key, out var index))
			{
				term = _terms[index];
				return true;
			}

			term = null;
			return false;
		}


		public SparqlStarObject(FrozenDictionary<string, int> termMapping, SparqlStarTerm[] terms)
		{
			_termMapping = termMapping;
			_terms = terms;
		}
	}
}
