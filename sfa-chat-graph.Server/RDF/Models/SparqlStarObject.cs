using sfa_chat_graph.Server.Utils;
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

		public IEnumerable<SparqlStarTerm> GetTerms() => _terms;
		public IEnumerable<(int index, SparqlStarTerm term)> GetIndexedTerms() => _terms.Enumerate();
		public IEnumerable<(string key, SparqlStarTerm term)> GetNamedTerms() => _termMapping.Select(key => (key.Key, _terms[key.Value]));


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

		private void ThrowIfConstructOutOfBounds(int count)
		{
			if (count >= _terms.Length)
				throw new ArgumentOutOfRangeException("Not enough values to deconstruct", nameof(count));
		}

		public void Desconstruct(out SparqlStarTerm firstTime)
		{
			ThrowIfConstructOutOfBounds(1);
			firstTime = _terms[0];
		}

		public void Desconstruct(out SparqlStarTerm firstTime, out SparqlStarTerm secondTerm)
		{
			ThrowIfConstructOutOfBounds(2);
			firstTime = _terms[0];
			secondTerm = _terms[1];
		}


		public void Desconstruct(out SparqlStarTerm firstTime, out SparqlStarTerm secondTerm, out SparqlStarTerm thirdTerm)
		{
			ThrowIfConstructOutOfBounds(3);
			firstTime = _terms[0];
			secondTerm = _terms[1];
			thirdTerm = _terms[2];
		}

		public void Desconstruct(out SparqlStarTerm firstTime, out SparqlStarTerm secondTerm, out SparqlStarTerm thirdTerm, out SparqlStarTerm fourthTerm)
		{
			ThrowIfConstructOutOfBounds(1);
			firstTime = _terms[0];
			secondTerm = _terms[1];
			thirdTerm = _terms[2];
			fourthTerm = _terms[3];
		}

		public void Desconstruct(out SparqlStarTerm firstTime, out SparqlStarTerm secondTerm, out SparqlStarTerm thirdTerm, out SparqlStarTerm fourthTerm, out SparqlStarTerm fifthTerm)
		{
			ThrowIfConstructOutOfBounds(1);
			firstTime = _terms[0];
			secondTerm = _terms[1];
			thirdTerm = _terms[2];
			fourthTerm = _terms[3];
			fifthTerm = _terms[4];
		}
	}
}
