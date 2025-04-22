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
		public Type ValueType => typeof(SparqlResultSet);

		private void SerializeNode(BsonSerializationContext context, BsonSerializationArgs args, INode node)
		{
			if (node == null)
			{
				context.Writer.WriteNull();
				return;
			}

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
			if (value == null)
			{
				context.Writer.WriteNull();
				return;
			}

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



		private INode DeserializeNode(IBsonReader reader, BsonDeserializationArgs args)
		{
			var token = reader.ReadBsonType();
			if (token == BsonType.Null)
			{
				reader.ReadNull();
				return null;
			}

			reader.ReadStartDocument();
			reader.ReadName("type");
			var type = (NodeType)reader.ReadInt32();
			try
			{

				switch (type)
				{
					case NodeType.Uri:
						reader.ReadName("value");
						var uri = reader.ReadString();
						return new UriNode(new Uri(uri));

					case NodeType.Literal:
						reader.ReadName("value");
						var value = reader.ReadString();
						string? language = null;
						Uri? datatype = null;
						if (reader.ReadName() == "datatype")
						{
							datatype = new Uri(reader.ReadString());
							return new LiteralNode(value, datatype);
						}
						else if (reader.ReadName() == "language")
						{
							language = reader.ReadString();
							return new LiteralNode(value, language);
						}

						return new LiteralNode(value);

					case NodeType.Blank:
						reader.ReadName("value");
						var id = reader.ReadString();
						return new BlankNode(id);

					case NodeType.Variable:
						reader.ReadName("variableName");
						var variableName = reader.ReadString();
						return new VariableNode(variableName);

					case NodeType.Triple:
						reader.ReadName("subject");
						var subject = DeserializeNode(reader, args);
						reader.ReadName("predicate");
						var predicate = DeserializeNode(reader, args);
						reader.ReadName("object");
						var obj = DeserializeNode(reader, args);
						return new TripleNode(new Triple(subject, predicate, obj));

					case NodeType.GraphLiteral:
						reader.ReadName("graph");
						var graph = BsonSerializer.Deserialize<IGraph>(reader);
						return new GraphLiteralNode(graph);

					default:
						throw new NotSupportedException($"Node type {type} is not supported.");
				}
			}
			finally
			{
				reader.ReadEndDocument();
			}
		}

		private SparqlResult DeserializeResult(IBsonReader reader, BsonDeserializationArgs args, string[] variables)
		{
			reader.ReadStartArray();
			var result = new SparqlResult();
			for (int i = 0; i < variables.Length; i++)
				result.SetValue(variables[i], DeserializeNode(reader, args));

			reader.ReadEndArray();
			return result;
		}

		private string[] ReadStringArray(IBsonReader reader, BsonDeserializationArgs args)
		{
			reader.ReadStartArray();
			var items = new List<string>();
			while (reader.ReadBsonType() != BsonType.EndOfDocument)
				items.Add(reader.ReadString());

			reader.ReadEndArray();
			return items.ToArray();
		}

		private SparqlResultSet ReadBooleanResult(IBsonReader reader)
		{
			reader.ReadName("result");
			return new SparqlResultSet(reader.ReadBoolean());
		}

		private SparqlResultSet ReadVariableBindingsResult(IBsonReader reader, BsonDeserializationArgs args)
		{
			reader.ReadName("variables");
			var variables = ReadStringArray(reader, args);
			reader.ReadName("results");
			reader.ReadStartArray();
			var results = new List<SparqlResult>();
			while (reader.ReadBsonType() != BsonType.EndOfDocument)
				results.Add(DeserializeResult(reader, args, variables));

			reader.ReadEndArray();
			return new SparqlResultSet(results);
		}

		private SparqlResultSet DeserializeImpl(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			var reader = context.Reader;
			var token = reader.ReadBsonType();
			if (token == BsonType.Null)
			{
				reader.ReadNull();
				return null;
			}

			reader.ReadStartDocument();	
			reader.ReadName("type");
			var type = (SparqlResultsType)reader.ReadInt32();
			SparqlResultSet result = type switch
			{
				SparqlResultsType.Unknown => new SparqlResultSet(),
				SparqlResultsType.Boolean => ReadBooleanResult(reader),
				SparqlResultsType.VariableBindings => ReadVariableBindingsResult(reader, args),
				_ => throw new NotSupportedException($"Sparql result type {type} is not supported.")
			};
			reader.ReadEndDocument();
			return result;
		}

		public SparqlResultSet Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => DeserializeImpl(context, args);
		object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => DeserializeImpl(context, args);
	}
}
