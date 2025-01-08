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

  constructor(private http: HttpClient) { 
    this.graph = new Graph();
    this.graph.createTripleLiteralObj("pflanzen/frucht/orange", "hat farbe", "orange");
    this.graph.createTripleLiteralObj("pflanzen/frucht/orange", "ist geniessbar", "Ja");
    this.graph.createTripleLiteralObj("pflanzen/frucht/orange", "enthält", "Vitamin C");
    this.graph.createTriple("pflanzen/frucht/orange", "wächst an", "pflanzen/baum/orangenbaum");
    this.graph.createTriple("pflanzen/frucht/orange", "ist", "pflanzen/frucht");
    this.graph.createTriple("pflanzen/frucht/orange", "synonym", "pflanzen/frucht/apfelsine");
    
    this.graph.createTriple("pflanzen/baum/orangenbaum", "ist", "pflanzen/baum/laubbaum");
    this.graph.createTripleLiteralObj("pflanzen/baum/orangenbaum", "lateinischer name", "Citrus × sinensis L");
    this.graph.createTripleLiteralObj("pflanzen/baum/orangenbaum", "wächst in", "subtropischen Klimazonen");

    this.graph.createTriple("pflanzen/frucht/apfelsine", "ist", "synonym");
    this.graph.createTriple("pflanzen/frucht/apfelsine", "synonym für", "pflanzen/frucht/orange");
    this.graph.createTripleLiteralObj("pflanzen/frucht/apfelsine", "region", "nördlich der Spreyerer Linie");

    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasName", "Weber")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasFirstName", "Hans Jakob")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasOrigin", "Fischental")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasReligion", "Reformiert")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasGender", "Mann")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasAge", "37")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasFamily", "1")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasMaritalStatus", "verheiratet")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasFamilySize", "3")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasChildren", "Hat Kinder")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasId", "130")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasOtherDuties", "1")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasBeenTeachingSince", "17")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "rdf:type", " https://ld.admin.ch/stapfer/stapfer/Teacher")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#teacherOfOccupation", "https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#teachesAtSchool", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#teachesAtSchool", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B40")


    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSalary", "https://ld.admin.ch/stapfer/stapfer/Salary/14")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSalary", "https://ld.admin.ch/stapfer/stapfer/Salary/15")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSchool", "https://ld.admin.ch/stapfer/stapfer/School/28")

    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/School/40", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/School");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasMainSchoolType", "0");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasReligion", "Reformiert");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolFee", "keine Angabe");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolId", "40");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolName", "Länzen");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolType", "Repetierschule");


    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/School/28", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/School");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasClassDivision", "1");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasClassDivisionType", "Pensenklasse");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasMainSchoolType", "1");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasReligion", "Reformiert");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolFee", "Ja");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolId", "28");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolName", "Länzen");
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolType", "Niedere Schule/Deutsche Schule");


    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B40", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B40", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSchool", "https://ld.admin.ch/stapfer/stapfer/School/40")


    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1", " https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/predicates#hasTeacherOccupationCategory", "Erstberuf")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/TeacherOccupation")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1", "https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/predicates#hasOccupation", "https://ld.admin.ch/stapfer/stapfer/Occupation/94")

    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Occupation/94", "https://ld.admin.ch/stapfer/stapfer/Occupation/predicates#hasOccupationId", "94")
    // this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Occupation/94", "https://ld.admin.ch/stapfer/stapfer/Occupation/predicates#hasNormalizedOccupdation", "Schneider")
    // this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Occupation/94", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/Occupation")

    this.graph.updateModels();
  }

  ngOnInit(): void {
  }



  title = 'sfa-chat-graph.client';
}
