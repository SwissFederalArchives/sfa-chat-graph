using MessagePack;
using MessagePack.Formatters;
using VDS.Common.Collections.Enumerations;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;

namespace sfa_chat_graph.Server.Utils.MessagePack
{
	public class SparqlResultSetFormatter : IMessagePackFormatter<SparqlResultSet>
	{
		private LiteralNode ReadLiteralNode(ref MessagePackReader reader)
		{
			var type = reader.ReadByte();
			switch (type)
			{
				case 0:
					return new LiteralNode(reader.ReadString());
				case 1:
					return new LiteralNode(reader.ReadString(), reader.ReadUri());
				case 2:
					return new LiteralNode(reader.ReadString(), reader.ReadString());
				default:
					throw new NotSupportedException($"Unsupported literal node type: {type}");
			}
		}

		private INode ReadNode(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			if(reader.TryReadNil())
			{
				return null;
			}

			var type = (NodeType)reader.ReadByte();
			switch (type)
			{
				case NodeType.Blank:
					var internalId = reader.ReadString();
					return new BlankNode(internalId);

				case NodeType.Uri:
					var uri = reader.ReadUri();
					return new UriNode(uri);

				case NodeType.Literal:
					return ReadLiteralNode(ref reader);

				case NodeType.Triple:
					var subject = ReadNode(ref reader, options);
					var predicate = ReadNode(ref reader, options);
					var obj = ReadNode(ref reader, options);
					return new TripleNode(new Triple(subject, predicate, obj));

				case NodeType.GraphLiteral:
					var subGraph = MessagePackSerializer.Deserialize<IGraph>(ref reader, options);
					return new GraphLiteralNode(subGraph);

				case NodeType.Variable:
					var variableName = reader.ReadString();
					return new VariableNode(variableName);

				default:
					throw new NotSupportedException($"Unsupported node type: {type}");
			}
		}

		private SparqlResultSet ReadBindingsResult(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			var variables = reader.ReadStringArray();
			var count = reader.ReadArrayHeader();
			var array = new SparqlResult[count];
			for (int i = 0; i < array.Length; i++)
			{
				var row = new SparqlResult();
				for (int j = 0; j < variables.Length; j++)
					row.SetValue(variables[j], ReadNode(ref reader, options));

				array[i] = row;
			}

			return new SparqlResultSet(array);
		}

		public SparqlResultSet Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
				return null;
			
			var type = (SparqlResultsType)reader.ReadByte();
			switch (type)
			{
				case SparqlResultsType.Unknown:
					return new SparqlResultSet();

				case SparqlResultsType.Boolean:
					var result = reader.ReadBoolean();
					return new SparqlResultSet(result);

				case SparqlResultsType.VariableBindings:
					return ReadBindingsResult(ref reader, options);

				default:
					throw new NotSupportedException($"Unsupported SparqlResultSet type: {type}");
			}

		}

		private void WriteLiteralNode(ref MessagePackWriter writer, LiteralNode node)
		{
			if (node.DataType.AbsoluteUri.Equals("http://www.w3.org/2001/XMLSchema#string"))
			{
				writer.Write((byte)0);
				writer.Write(node.Value);
			}
			else
			if (node.Language == string.Empty)
			{
				writer.Write((byte)1);
				writer.Write(node.Value);
				writer.Write(node.DataType.AbsoluteUri);
			}
			else
			{
				writer.Write((byte)2);
				writer.Write(node.Value);
				writer.Write(node.Language);
			}
		}

		private void WriteNode(ref MessagePackWriter writer, MessagePackSerializerOptions options, INode node)
		{
			if(node == null)
			{
				writer.WriteNil();
				return;
			}

			writer.Write((byte)node.NodeType);
			switch (node)
			{
				case UriNode uriNode:
					writer.Write(uriNode.Uri.ToString());
					break;

				case LiteralNode literalNode:
					WriteLiteralNode(ref writer, literalNode);
					break;


				case BlankNode blankNode:
					writer.Write(blankNode.InternalID);
					break;

				case VariableNode variableNode:
					writer.Write(variableNode.VariableName);
					break;

				case TripleNode tripleNode:
					WriteNode(ref writer, options, tripleNode.Triple.Subject);
					WriteNode(ref writer, options, tripleNode.Triple.Predicate);
					WriteNode(ref writer, options, tripleNode.Triple.Object);
					break;

				case GraphLiteralNode graphLiteralNode:
					MessagePackSerializer.Serialize(ref writer, graphLiteralNode.SubGraph, options);
					break;
			}
		}

		public void Serialize(ref MessagePackWriter writer, SparqlResultSet value, MessagePackSerializerOptions options)
		{
			if(value == null)
			{
				writer.WriteNil();
				return;
			}

			writer.Write((byte)value.ResultsType);
			if (value.ResultsType == SparqlResultsType.VariableBindings)
			{
				var variables = value.Variables.ToArray();
				writer.WriteStringArray(variables);
				writer.WriteArrayHeader(value.Results.Count);
				foreach (var result in value.Results)
					for (int i = 0; i < variables.Length; i++)
						WriteNode(ref writer, options, result[variables[i]]);

			}
			else if (value.ResultsType == SparqlResultsType.Boolean)
			{
				writer.Write(value.Result);
			}
		}
	}
}
