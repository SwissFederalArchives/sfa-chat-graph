import { AfterViewInit, Component, ElementRef, Input, ViewChild } from '@angular/core';
import { Graph } from '../graph/graph';
import { NaiveGraphLayout } from '../graph/naive-layout';
import { IGraphLayout } from '../graph/graph-layout';

@Component({
  selector: 'graph',
  imports: [],
  standalone: true,
  templateUrl: './graph-visualisation.component.html',
  styleUrl: './graph-visualisation.component.css'
})
export class GraphVisualisationComponent implements AfterViewInit {

  @Input() graph!: Graph;
  @Input() width: number = 500;
  @Input() height: number = 500;
  @ViewChild("canvas", { static: false }) canvas!: ElementRef;
  private _ctx?: CanvasRenderingContext2D;
  private _layouting: IGraphLayout;

  constructor(layouting?: IGraphLayout) {
    this._layouting = layouting ?? new NaiveGraphLayout();
    this.graph = new Graph();
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasName", "Weber")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasFirstName", "Hans Jakob")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasOrigin", "Fischental")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasReligion", "Reformiert")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasGender", "Mann")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasAge", "37")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasFamily", "1")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasMaritalStatus", "verheiratet")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasFamilySize", "3")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasChildren", "Hat Kinder")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasId", "130")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasOtherDuties", "1")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#hasBeenTeachingSince", "17")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "rdf:type", " https://ld.admin.ch/stapfer/stapfer/Teacher")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#teacherOfOccupation", " https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#teachesAtSchool", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Teacher/130", "https://ld.admin.ch/stapfer/stapfer/Teacher/predicates#teachesAtSchool", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B40")


    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSalary", "https://ld.admin.ch/stapfer/stapfer/Salary/14")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSalary", "https://ld.admin.ch/stapfer/stapfer/Salary/15")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B28", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSchool", "https://ld.admin.ch/stapfer/stapfer/School/28")

    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/School/40", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/School");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasMainSchoolType", "0");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasReligion", "Reformiert");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolFee", "keine Angabe");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolId", "40");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolName", "Länzen");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/40", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolType", "Repetierschule");


    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/School/28", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/School");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasClassDivision", "1");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasClassDivisionType", "Pensenklasse");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasMainSchoolType", "1");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasReligion", "Reformiert");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolFee", "Ja");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolId", "28");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolName", "Länzen");
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/School/28", "https://ld.admin.ch/stapfer/stapfer/School/predicates#hasSchoolType", "Niedere Schule/Deutsche Schule");


    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B40", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherSchool/130%2B40", "https://ld.admin.ch/stapfer/stapfer/TeacherSchool/predicates#belongsToSchool", "https://ld.admin.ch/stapfer/stapfer/School/40")


    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1", " https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/predicates#hasTeacherOccupationCategory", "Erstberuf")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/TeacherOccupation")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/1", "https://ld.admin.ch/stapfer/stapfer/TeacherOccupation/predicates#hasOccupation", "https://ld.admin.ch/stapfer/stapfer/Occupation/94")

    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Occupation/94", "https://ld.admin.ch/stapfer/stapfer/Occupation/predicates#hasOccupationId", "94")
    this.graph.createTripleLiteralObj("https://ld.admin.ch/stapfer/stapfer/Occupation/94", "https://ld.admin.ch/stapfer/stapfer/Occupation/predicates#hasNormalizedOccupdation", "Schneider")
    this.graph.createTriple("https://ld.admin.ch/stapfer/stapfer/Occupation/94", "rdf:type", "https://ld.admin.ch/stapfer/stapfer/Occupation")

    this.graph.updateModels();
  }

  ngAfterViewInit() {
    this._ctx = this.canvas.nativeElement.getContext("2d");
    this.render();  
  }

  render() {
    if (this._ctx) {
      this._layouting.layout(this.graph);
      for (const edge of this.graph.getEdges()) {
        this._ctx.beginPath();
        this._ctx.strokeStyle = edge.color;
        this._ctx.moveTo(edge.getFrom().x, edge.getFrom().y);
        this._ctx.lineTo(edge.getTo().x, edge.getTo().y);
        this._ctx.stroke();

        this._ctx.save();
        const midX = (edge.getFrom().x + edge.getTo().x) / 2;
        const midY = (edge.getFrom().y + edge.getTo().y) / 2;
        const name = edge.label.split("/").slice(-2).join('/');
        const angle = Math.atan2(edge.getTo().y - edge.getFrom().y, edge.getTo().x - edge.getFrom().x);
        this._ctx.translate(midX, midY);
        this._ctx.rotate(angle);
        this._ctx.fillStyle = "#202020";
        this._ctx.fillText(name, 0, 0);
        this._ctx.restore();
      }

      this._ctx.textAlign = "center"
      for (const node of this.graph.getNodes()) {
        this._ctx.beginPath();
        this._ctx.fillStyle = node.color;
        this._ctx.arc(node.x, node.y, node.radius, 0, 2 * Math.PI, false);
        this._ctx.closePath();
        this._ctx.fill();

        const name = node.label.split("/").slice(-2).join('/');
        this._ctx.fillStyle = "#202020";
        this._ctx.fillText(name, node.x, node.y, node.radius * 2);
      }
    }
  }
}
