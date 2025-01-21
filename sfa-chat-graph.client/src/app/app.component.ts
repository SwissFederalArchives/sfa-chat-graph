import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Graph } from './graph/graph';
import { firstValueFrom } from 'rxjs';
import { ApiClientService } from './services/api-client/api-client.service';

const DATABASE = "TestDB";
const MAX_LOAD_COUNT: number = 15;

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {


  graph?: Graph;

  async queryGraph(iri: string, maxDepth: number, noFollow: string[] = [], maxChildren: number = 25): Promise<Graph> {
    const graph = new Graph();
    const iriStack = [{ iri: iri, depth: 0 }]
    const visited = new Set<string>();
    noFollow.forEach(value => visited.add(value));
    visited.add(iri);
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/x-www-form-urlencoded')
      .set("Accept", "application/x-graphdb-table-results+json");

    while (iriStack.length > 0) {
      const currentSubject = iriStack.pop()!;
      const body = new HttpParams()
        .set("query", `DESCRIBE <${currentSubject?.iri}>`);


      let response = await firstValueFrom(this.http.post<any>(`http://localhost:40112/repositories/${DATABASE}`, body, { headers: headers, responseType: "json" }));
      if (response.results.bindings.length > maxChildren)
        continue;

      for (var key in response.results.bindings) {
        const item = response.results.bindings[key];
        const sub = item.subject.value;
        const pred = item.predicate.value;
        const obj = item.object.value;
        if (item.object.type == "uri") {
          const created = graph.createTriple(sub, pred, obj);
          if (currentSubject.depth < maxDepth && pred != "http://www.w3.org/1999/02/22-rdf-syntax-ns#type" && visited.has(obj) == false) {
            iriStack.push({ iri: obj, depth: currentSubject.depth + 1 })
            visited.add(obj);
            created.sub.markLeafsLoaded();
          }
        } else {
          const created = graph.createTripleLiteralObj(sub, pred, obj);
          created.sub.markLeafsLoaded();
        }
      }
    }

    graph.onNodeDetailsRequested.subscribe(async (data: { value: any, next: () => void }) => {
      const body = new HttpParams()
        .set('Content-Type', 'application/x-www-form-urlencoded')
        .set("query", `DESCRIBE <${data.value.node.id}>`);

      let response = await firstValueFrom(this.http.post<any>(`http://localhost:40112/repositories/${DATABASE}`, body, { headers: headers, responseType: "json" }));
      let childCount = 0;
      for (var key in response.results.bindings.sort((a: any, b: any) => a.object.type.localeCompare(b.object.type))) {
        const item = response.results.bindings[key];
        const sub = item.subject.value;
        const pred = item.predicate.value;
        const obj = item.object.value;
        if (item.object.type == "uri") {
          const created = graph.createTriple(sub, pred, obj);
          if (childCount++ > MAX_LOAD_COUNT) {
            if (created.subCreated)
              created.sub.setHidden(true);

            if (created.objCreated)
              created.obj.setHidden(true);
          }
        } else {
          const created = graph.createTripleLiteralObj(sub, pred, obj);
          if (childCount++ > MAX_LOAD_COUNT) {
            if (created.subCreated)
              created.sub.setHidden(true);

            if (created.objCreated)
              created.obj.setHidden(true);
          }
        }
      }

      data.next();
    });

    return graph;
  }

  getSimpleGraph(): Graph {
    const graph = new Graph();
    graph.createTripleLiteralObj("pflanzen/frucht/orange", "hat farbe", "orange");
    graph.createTripleLiteralObj("pflanzen/frucht/orange", "ist geniessbar", "Ja");
    graph.createTripleLiteralObj("pflanzen/frucht/orange", "enthält", "Vitamin C");
    graph.createTriple("pflanzen/frucht/orange", "wächst an", "pflanzen/baum/orangenbaum");
    graph.createTriple("pflanzen/frucht/orange", "ist", "pflanzen/frucht");
    graph.createTriple("pflanzen/frucht/orange", "synonym", "pflanzen/frucht/apfelsine");
    graph.createTriple("pflanzen/baum/orangenbaum", "ist", "pflanzen/baum/laubbaum");
    graph.createTripleLiteralObj("pflanzen/baum/orangenbaum", "lateinischer name", "Citrus x sinensis L");
    graph.createTripleLiteralObj("pflanzen/baum/orangenbaum", "wächst in", "subtropischen Klimazonen");
    graph.createTriple("pflanzen/frucht/apfelsine", "ist", "synonym");
    graph.createTriple("pflanzen/frucht/apfelsine", "synonym für", "pflanzen/frucht/orange");
    graph.createTripleLiteralObj("pflanzen/frucht/apfelsine", "region", "nördlich der Spreyerer Linie");
    graph.updateModels();
    return graph;
  }

  getComplexGraph(): Graph {
    const graph = new Graph();
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasName", "Weber")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasFirstName", "Hans Jakob")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasOrigin", "Fischental")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasReligion", "Reformiert")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasGender", "Mann")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasAge", "37")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasFamily", "1")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasMaritalStatus", "verheiratet")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasFamilySize", "3")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasChildren", "Hat Kinder")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasId", "130")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasOtherDuties", "1")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasBeenTeachingSince", "17")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "rdf:type", " https://ld.admin.ch/stapfer/stapfer/Teacher")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#teacherOfOccupation", "https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#teachesAtSchool", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#teachesAtSchool", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B40")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSalary", "https://ld.admin.ch/stapfer/stapfer/Salary/14")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSalary", "https://ld.admin.ch/stapfer/stapfer/Salary/15")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSchool", "https://ld.admin.ch/stapfer/stapfer/School/28")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/School/40", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/School");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasMainSchoolType", "0");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasReligion", "Reformiert");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolFee", "keine Angabe");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolId", "40");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolName", "Länzen");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolType", "Repetierschule");
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/School/28", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/School");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasClassDivision", "1");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasClassDivisionType", "Pensenklasse");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasMainSchoolType", "1");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasReligion", "Reformiert");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolFee", "Ja");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolId", "28");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolName", "Länzen");
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolType", "Niedere Schule/Deutsche Schule");
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B40", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B40", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSchool", "https://ld.admin.ch/stapfer/stapfer/School/40")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1", " https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/predicates#hasTeacherOccupationCategory", "Erstberuf")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/TeacherOccupation")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1", "https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/predicates#hasOccupation", "https://ld.admin.ch/stapfer/stapfer/Occupation/94")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Occupation/94", "https://ld.admin.ch/stapfer/stapfer/Occupation/predicates#hasOccupationId", "94")
    graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Occupation/94", "https://ld.admin.ch/stapfer/stapfer/Occupation/predicates#hasNormalizedOccupdation", "Schneider")
    graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Occupation/94", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/Occupation")
    graph.updateModels();
    return graph;
  }

  constructor(private http: HttpClient, private _apiClient: ApiClientService) {
    this.graph = new Graph();
    this.graph.onNodeDetailsRequested.subscribe(async (data) => {
      if (data.value) {
        const graph = data.value.graph;
        let response = await this._apiClient.describeAsync(data.value.node.id); 
        graph.loadFromSparqlStar(response, 20, data.value.node.subGraphId, response.head.vars);
        data.next(graph);
      }
    });
  }

  async ngOnInit() {
    // const graph = await this.queryGraph("https://ld.admin.ch/stapfer/stapfer/Teacher/367", 6, ["https://ld.admin.ch/stapfer/stapfer/Occupation/94", "https://ld.admin.ch/stapfer/stapfer/Transcription/18", "https://ld.admin.ch/stapfer/stapfer/SchoolType/1"]);
    // graph.updateModels();
    // this.graph = graph;
  }



  title = 'sfa-chat-graph.client';
}
