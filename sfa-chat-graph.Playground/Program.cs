using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using sfa_chat_graph.Server.RDF;
using System.Text;
using VDS.RDF;
using VDS.RDF.Dynamic;
using VDS.RDF.Nodes;
using VDS.RDF.Query;
using VDS.RDF.Storage;

var server = new OntotextStorageServer("http://localhost:7200");
var db = await server.GetStoreAsync("TestDB") as IAsyncQueryableStorage;

//var graphs = (await db.ListGraphsAsync(CancellationToken.None)).ToArray();

var query = $$"""
		select distinct ?st ?p ?ot where { 
	    graph <http://ld.admin.ch/stapfer-ai> {
	       ?s a ?st .
	       optional { 
	            ?s ?p ?o .
	       		optional { 
					?o a ?ot . 
				}  
	        }
	    }
	}
""";

var rdfType = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");

var data = await db.QueryAsync(query, CancellationToken.None) as SparqlResultSet;
var builder = new StringBuilder();
foreach (var group in data.Results.GroupBy(x => (x["st"] as IUriNode).Uri))
{
	builder.AppendLine($"{group.Key}: [");
	foreach (var row in group)
	{
		var predicate = (row["p"] as IUriNode).Uri;
		if (predicate.Equals(rdfType))
			continue;
		
		var ot = row["ot"];
		builder.Append($"\t{predicate} -> ");
		switch (ot)
		{
			case IUriNode uriNode:
					builder.Append(uriNode.Uri);
				break;

			default:
				builder.Append("literal");
				break;
		}

		builder.AppendLine();
	}
	builder.AppendLine("]");
	builder.AppendLine();
}

Console.WriteLine(builder.ToString());
