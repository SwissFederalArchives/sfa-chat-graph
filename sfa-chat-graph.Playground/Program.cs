using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using SfaChatGraph.Server.RDF;
using System.Text;
using VDS.RDF;
using VDS.RDF.Dynamic;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Storage;


var query = """
PREFIX teacher: <https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#>
PREFIX ts: <https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#>

SELECT ?teacher ?teacherName (COUNT(?school) AS ?schoolCount)
WHERE { 
    ?teacher teacher:hasTeacher ?teacherSchool .
    ?teacherSchool ts:belongsToSchool ?school .
    ?teacher teacher:hasName ?teacherName .}
GROUP BY ?teacher ?teacherName 
ORDER BY DESC(?schoolCount)
LIMIT 1
""";

var parser = new SparqlQueryParser();
var sparql = parser.ParseFromString(query);
Console.WriteLine(sparql.ToString());
