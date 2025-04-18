using AngleSharp.Io;
using AwosFramework.ApiClients.Jupyter.Client;
using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models;
using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket;
using AwosFramework.ApiClients.Jupyter.WebSocket.Terminal.Protocol;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.DataAnnotations;
using MessagePack;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.RDF;
using sfa_chat_graph.Server.RDF.Endpoints;
using sfa_chat_graph.Server.Utils;
using sfa_chat_graph.Server.Utils.Json;
using sfa_chat_graph.Server.Utils.MessagePack;
using SfaChatGraph.Server.RDF;
using System.Buffers.Text;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.Json;
using VDS.RDF;
using VDS.RDF.Dynamic;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Storage;

var query = """
	SELECT ?customerSegment ?label ?identifier ?travellerType ?minAge ?maxAge ?gender WHERE { GRAPH <https://lindas.admin.ch/sbb/nova> { ?customerSegment a <https://lod.opentransportdata.swiss/vocab/CustomerSegment> . ?customerSegment <http://www.w3.org/2000/01/rdf-schema#label> ?label . ?customerSegment <http://schema.org/identifier> ?identifier . OPTIONAL { ?customerSegment <https://lod.opentransportdata.swiss/vocab/travellerType> ?travellerType . } OPTIONAL { ?customerSegment <https://lod.opentransportdata.swiss/vocab/minAge> ?minAge . } OPTIONAL { ?customerSegment <https://lod.opentransportdata.swiss/vocab/maxAge> ?maxAge . } OPTIONAL { ?customerSegment <http://xmlns.com/foaf/0.1/#gender> ?gender . } } }
	""";

var graphJson = """
{
  "type": "VariableBindings",
  "head": [
    "customerSegment",
    "label",
    "identifier",
    "travellerType",
    "minAge",
    "maxAge",
    "gender"
  ],
  "results": {
    "bindings": [
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/hund"
        },
        "label": {
          "type": "literal",
          "value": "Hund",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/hund"
        },
        "label": {
          "type": "literal",
          "value": "Chien",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/hund"
        },
        "label": {
          "type": "literal",
          "value": "Cane",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/hund"
        },
        "label": {
          "type": "literal",
          "value": "Dog",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/hund-abo"
        },
        "label": {
          "type": "literal",
          "value": "Hund",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "HUND_ABO",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/hund-abo"
        },
        "label": {
          "type": "literal",
          "value": "Chien",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "HUND_ABO",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/hund-abo"
        },
        "label": {
          "type": "literal",
          "value": "Cane",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "HUND_ABO",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/hund-abo"
        },
        "label": {
          "type": "literal",
          "value": "Dog",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "HUND_ABO",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "HUND",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/iv-ai"
        },
        "label": {
          "type": "literal",
          "value": "Reisende mit Behinderung",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "IV_AI",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/iv-ai"
        },
        "label": {
          "type": "literal",
          "value": "Personne avec handicap",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "IV_AI",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/iv-ai"
        },
        "label": {
          "type": "literal",
          "value": "per viaggiatori disabili",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "IV_AI",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/iv-ai"
        },
        "label": {
          "type": "literal",
          "value": "for Disabled Persons",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "IV_AI",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/kind-6-16"
        },
        "label": {
          "type": "literal",
          "value": "Kind 6-16",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "KIND_6-16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "6",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/kind-6-16"
        },
        "label": {
          "type": "literal",
          "value": "Enfant 6-16",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "KIND_6-16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "6",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/kind-6-16"
        },
        "label": {
          "type": "literal",
          "value": "Ragazzi 6-16",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "KIND_6-16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "6",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/kind-6-16"
        },
        "label": {
          "type": "literal",
          "value": "Children 6-16",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "KIND_6-16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "6",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-25-64-dame"
        },
        "label": {
          "type": "literal",
          "value": "Erwachsene",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_25-64_DAME",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "25",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "64",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": {
          "type": "literal",
          "value": "WEIBLICH",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        }
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-25-64-dame"
        },
        "label": {
          "type": "literal",
          "value": "Adulti",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_25-64_DAME",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "25",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "64",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": {
          "type": "literal",
          "value": "WEIBLICH",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        }
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-25-64-dame"
        },
        "label": {
          "type": "literal",
          "value": "Adults",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_25-64_DAME",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "25",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "64",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": {
          "type": "literal",
          "value": "WEIBLICH",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        }
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-25-64-dame"
        },
        "label": {
          "type": "literal",
          "value": "Adulte",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_25-64_DAME",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "25",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "64",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": {
          "type": "literal",
          "value": "WEIBLICH",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        }
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-25-65-herr"
        },
        "label": {
          "type": "literal",
          "value": "Erwachsene",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_25-65_HERR",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "25",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "65",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": {
          "type": "literal",
          "value": "MAENNLICH",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        }
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-25-65-herr"
        },
        "label": {
          "type": "literal",
          "value": "Adulti",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_25-65_HERR",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "25",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "65",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": {
          "type": "literal",
          "value": "MAENNLICH",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        }
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-25-65-herr"
        },
        "label": {
          "type": "literal",
          "value": "Adults",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_25-65_HERR",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "25",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "65",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": {
          "type": "literal",
          "value": "MAENNLICH",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        }
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-25-65-herr"
        },
        "label": {
          "type": "literal",
          "value": "Adulte",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_25-65_HERR",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "25",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": {
          "type": "literal",
          "value": "65",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "gender": {
          "type": "literal",
          "value": "MAENNLICH",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        }
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/velo-vollpreis"
        },
        "label": {
          "type": "literal",
          "value": "Velo",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "VELO_VOLLPREIS",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "VELO",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/velo-vollpreis"
        },
        "label": {
          "type": "literal",
          "value": "Vélo",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "VELO_VOLLPREIS",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "VELO",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/velo-vollpreis"
        },
        "label": {
          "type": "literal",
          "value": "Bici",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "VELO_VOLLPREIS",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "VELO",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/velo-vollpreis"
        },
        "label": {
          "type": "literal",
          "value": "Bike",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "VELO_VOLLPREIS",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "VELO",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/ga-1-kl"
        },
        "label": {
          "type": "literal",
          "value": "GA 1. Klasse",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "GA_1_KL",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/ga-1-kl"
        },
        "label": {
          "type": "literal",
          "value": "AG 1ère classe",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "GA_1_KL",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/ga-1-kl"
        },
        "label": {
          "type": "literal",
          "value": "AG 1ª classe",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "GA_1_KL",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/ga-1-kl"
        },
        "label": {
          "type": "literal",
          "value": "GA Travelcard 1st class",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "GA_1_KL",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/ga-2-kl"
        },
        "label": {
          "type": "literal",
          "value": "GA 2. Klasse",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "GA_2_KL",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/ga-2-kl"
        },
        "label": {
          "type": "literal",
          "value": "AG 2ème classe",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "GA_2_KL",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/ga-2-kl"
        },
        "label": {
          "type": "literal",
          "value": "AG 2ª classe",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "GA_2_KL",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/ga-2-kl"
        },
        "label": {
          "type": "literal",
          "value": "GA Travelcard 2nd class",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "GA_2_KL",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": null,
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/halbtax"
        },
        "label": {
          "type": "literal",
          "value": "Halbtax-Abo",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "HALBTAX",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/halbtax"
        },
        "label": {
          "type": "literal",
          "value": "Abonnement demi-tarif",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "HALBTAX",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/halbtax"
        },
        "label": {
          "type": "literal",
          "value": "Abbonamento metà-prezzo",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "HALBTAX",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/halbtax"
        },
        "label": {
          "type": "literal",
          "value": "Half-Fare Travelcard",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "HALBTAX",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-16"
        },
        "label": {
          "type": "literal",
          "value": "Erwachsene",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "de"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_16+",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-16"
        },
        "label": {
          "type": "literal",
          "value": "Adulti",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "it"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_16+",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-16"
        },
        "label": {
          "type": "literal",
          "value": "Adults",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "en"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_16+",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      },
      {
        "customerSegment": {
          "type": "uri",
          "value": "https://lod.opentransportdata.swiss/customersegment/person-16"
        },
        "label": {
          "type": "literal",
          "value": "Adulte",
          "datatype": "http://www.w3.org/1999/02/22-rdf-syntax-ns#langString",
          "xml:lang": "fr"
        },
        "identifier": {
          "type": "literal",
          "value": "PERSON_16+",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "travellerType": {
          "type": "literal",
          "value": "PERSON",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "minAge": {
          "type": "literal",
          "value": "16",
          "datatype": "http://www.w3.org/2001/XMLSchema#string"
        },
        "maxAge": null,
        "gender": null
      }
    ]
  }
}
""";
var options = new JsonSerializerOptions();
options.Converters.Add(new SparqlResultSetConverter());
var resultSet = JsonSerializer.Deserialize<SparqlResultSet>(graphJson, options);

var msg = new ApiToolResponseMessage
{
  Id = Guid.NewGuid(),
  Content = "Test message",
  ToolCallId = "ToolId",
	TimeStamp = DateTime.UtcNow,
  GraphToolData = new ApiGraphToolData
  {
    Query = "Sparql Query",
		DataGraph = resultSet,
    VisualisationGraph = resultSet
	}
};

var result = new SparqlResult();
result.SetValue("?a", new LiteralNode("test"));
result.SetValue("?b", new UriNode(new Uri("http://example.com/test/node1")));
result.SetValue("?c", null);
result.SetValue("?d", null);

var result2 = new SparqlResult();
result2.SetValue("?a", new LiteralNode("test2"));
result2.SetValue("?b", new UriNode(new Uri("http://example.com/test/node3")));
result2.SetValue("?c", null);
result2.SetValue("?d", new UriNode(new Uri("http://example.com/test/node4")));

//resultSet = new SparqlResultSet([result, result2]);

using var stream = new MemoryStream();
var msgPackOptions = new MessagePackSerializerOptions(FormatterResolver.Instance)
	.WithCompression(MessagePackCompression.Lz4BlockArray);

MessagePackSerializer.Serialize(stream, resultSet, msgPackOptions);
stream.Position = 0;
var dMsg = MessagePackSerializer.Deserialize<SparqlResultSet>(stream, msgPackOptions); 

Console.WriteLine("Done");