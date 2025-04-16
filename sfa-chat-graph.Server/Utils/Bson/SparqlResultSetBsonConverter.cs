using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using SfaChatGraph.Server.RDF.Models;
using VDS.RDF;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Utils.Bson
{
	public class SparqlResultSetBsonConverter : IBsonSerializer<SparqlResultSet>
	{
		public Type ValueType => typeof(SparqlStarResult);

		private void SerializeNode(BsonSerializationContext context, BsonSerializationArgs args, INode node)
		{
			context.Writer.WriteStartDocument();
			context.Writer.WriteName("type");
			context.Writer.WriteInt32((int)node.NodeType);
			switch (node)
			{
				case UriNode uriNode:
					context.Writer.WriteName("value");
					context.Writer.WriteString(uriNode.Uri.ToString());
					break;

				case LiteralNode literalNode:
					context.Writer.WriteName("value");
					context.Writer.WriteString(literalNode.Value);
					if (string.IsNullOrEmpty(literalNode.Language))
					{
						context.Writer.WriteName("datatype");
						context.Writer.WriteString(literalNode.DataType.ToString());
					}
					else
					{
						context.Writer.WriteName("language");
						context.Writer.WriteString(literalNode.Language);
					}
					break;

				case BlankNode blankNode:
					context.Writer.WriteName("value");
					context.Writer.WriteString(blankNode.InternalID);
					break;

				case TripleNode tripleNode:
					context.Writer.WriteName("subject");
					SerializeNode(context, args, tripleNode.Triple.Subject);
					context.Writer.WriteName("predicate");
					SerializeNode(context, args, tripleNode.Triple.Predicate);
					context.Writer.WriteName("object");
					SerializeNode(context, args, tripleNode.Triple.Object);
					break;

				case VariableNode variableNode:
					context.Writer.WriteName("variableName");
					context.Writer.WriteString(variableNode.VariableName);
					break;

				case GraphLiteralNode graphLiteralNode:
					context.Writer.WriteName("graph");
					BsonSerializer.Serialize(context.Writer, graphLiteralNode.SubGraph, args: args);
					break;
			}
			context.Writer.WriteEndDocument();
		}

		private void SerializeStringArray(BsonSerializationContext context, IEnumerable<string> items)
		{
			context.Writer.WriteStartArray();
			foreach (var variable in items)
				context.Writer.WriteString(variable);

			context.Writer.WriteEndArray();
		}

		private void SerializeResult(BsonSerializationContext context, BsonSerializationArgs args, string[] variables, ISparqlResult value)
		{
			context.Writer.WriteStartArray();
			for (int i = 0; i < variables.Length; i++)
				SerializeNode(context, args, value[variables[i]]);

			context.Writer.WriteEndArray();
		}

		private void SerializeImpl(BsonSerializationContext context, BsonSerializationArgs args, SparqlResultSet value)
		{
			context.Writer.WriteStartDocument();
			context.Writer.WriteName("type");
			context.Writer.WriteInt32((int)value.ResultsType);

			if (value.ResultsType == SparqlResultsType.VariableBindings)
			{
				context.Writer.WriteName("variables");
				var variables = value.Variables.ToArray();
				SerializeStringArray(context, variables);
				context.Writer.WriteName("results");
				context.Writer.WriteStartArray();
				foreach (var result in value.Results)
					SerializeResult(context, args, variables, result);

				context.Writer.WriteEndArray();
				context.Writer.WriteEndDocument();
			}
			else if (value.ResultsType == SparqlResultsType.Boolean)
			{
				context.Writer.WriteName("result");
				context.Writer.WriteBoolean(value.Result);
			}

			context.Writer.WriteEndDocument();
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, SparqlResultSet value) => SerializeImpl(context, args, value);
		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => SerializeImpl(context, args, (SparqlResultSet)value);



		private INode DeserializeNode(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			context.Reader.ReadStartDocument();
			context.Reader.ReadName("type");
			var type = (NodeType)context.Reader.ReadInt32();
			try
			{

				switch (type)
				{
					case NodeType.Uri:
						context.Reader.ReadName("value");
						var uri = context.Reader.ReadString();
						return new UriNode(new Uri(uri));

					case NodeType.Literal:
						context.Reader.ReadName("value");
						var value = context.Reader.ReadString();
						string? language = null;
						Uri? datatype = null;
						if (context.Reader.ReadName() == "datatype")
						{
							datatype = new Uri(context.Reader.ReadString());
							return new LiteralNode(value, datatype);
						}
						else if (context.Reader.ReadName() == "language")
						{
							language = context.Reader.ReadString();
							return new LiteralNode(value, language);
						}

						return new LiteralNode(value);

					case NodeType.Blank:
						context.Reader.ReadName("value");
						var id = context.Reader.ReadString();
						return new BlankNode(id);

					case NodeType.Variable:
						context.Reader.ReadName("variableName");
						var variableName = context.Reader.ReadString();
						return new VariableNode(variableName);

					case NodeType.Triple:
						context.Reader.ReadName("subject");
						var subject = DeserializeNode(context, args);
						context.Reader.ReadName("predicate");
						var predicate = DeserializeNode(context, args);
						context.Reader.ReadName("object");
						var obj = DeserializeNode(context, args);
						return new TripleNode(new Triple(subject, predicate, obj));

					case NodeType.GraphLiteral:
						context.Reader.ReadName("graph");
						var graph = BsonSerializer.Deserialize<IGraph>(context.Reader);
						return new GraphLiteralNode(graph);

					default:
						throw new NotSupportedException($"Node type {type} is not supported.");
				}
			}
			finally
			{
				context.Reader.ReadEndDocument();
			}
		}

		private SparqlResult DeserializeResult(BsonDeserializationContext context, BsonDeserializationArgs args, string[] variables)
		{
			context.Reader.ReadStartArray();
			var result = new SparqlResult();
			for (int i = 0; i < variables.Length; i++)
				result.SetValue(variables[i], DeserializeNode(context, args));
			
			context.Reader.ReadEndArray();
			return result;
		}

		private string[] ReadStringArray(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			context.Reader.ReadStartArray();
			var items = new List<string>();
			while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
				items.Add(context.Reader.ReadString());

			context.Reader.ReadEndArray();
			return items.ToArray();
		}

		private SparqlResultSet DeserializeImpl(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			context.Reader.ReadStartDocument();
			context.Reader.ReadName("type");
			var type = (SparqlResultsType)context.Reader.ReadInt32();
			if (type == SparqlResultsType.VariableBindings)
			{
				context.Reader.ReadName("variables");
				var variables = ReadStringArray(context, args);
				context.Reader.ReadName("results");
				context.Reader.ReadStartArray();
				var results = new List<SparqlResult>();
				while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
					results.Add(DeserializeResult(context, args, variables));
				context.Reader.ReadEndArray();
				return new SparqlResultSet(results);
			}
			else if (type == SparqlResultsType.Boolean)
			{
				context.Reader.ReadName("result");
				return new SparqlResultSet(context.Reader.ReadBoolean());
			}
			throw new NotSupportedException($"Sparql result type {type} is not supported.");
		}

		public SparqlResultSet Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => DeserializeImpl(context, args);
		object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => DeserializeImpl(context, args);
	}
}
