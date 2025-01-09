import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Graph } from './graph/graph';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.css'
})

export class AppComponent implements OnInit {

  graph: Graph;

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
    return graph;
  }

  constructor(private http: HttpClient) {
    this.graph = this.getComplexGraph();
    this.graph.updateModels();
  }

  ngOnInit(): void {
  }



  title = 'sfa-chat-graph.client';
}
