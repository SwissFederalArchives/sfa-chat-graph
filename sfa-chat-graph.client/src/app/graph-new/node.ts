import { ElementRef, EventEmitter } from "@angular/core";
import { Vector } from "./vector";
import { from } from "rxjs";


const svgNs: string = "http://www.w3.org/2000/svg";
const nodeRadius: number = 50;
const radiusText: string = nodeRadius.toString();

export abstract class NodeBase {
  public positionChanged: EventEmitter<Vector> = new EventEmitter<Vector>();
  public color: string;
  public nextPosition: Vector;
  protected position: Vector;
  protected hidden: boolean = false;
  protected nativeElement?: SVGGElement;
  protected nativeTransform?: SVGTransform;
  public label: string;

  public abstract getRadius(): number;

  constructor(label: string, color: string, position?: Vector) {
    this.color = color;
    this.label = label;
    this.position = position ?? Vector.zero();
    this.nextPosition = Vector.zero();
  }

  abstract setupChildren(svg: ElementRef<SVGSVGElement>, group: SVGGElement): void;

  public setup(svg: ElementRef<SVGSVGElement>): SVGGElement {
    const circle = document.createElementNS(svgNs, "circle");
    circle.setAttribute("r", radiusText);
    circle.setAttribute("fill", this.color);

    const label = document.createElementNS(svgNs, "text");
    label.textContent = this.label;
    label.setAttribute("dominant-baseline", "middle");
    label.setAttribute("x", radiusText);
    label.setAttribute("y", radiusText);
    label.setAttribute("font-size", "20");
    label.setAttribute("fill", "black");

    const group: SVGGElement = document.createElementNS(svgNs, "g") as SVGGElement;
    group.appendChild(circle);
    group.appendChild(label);

    const groupTransform: SVGTransform = svg.nativeElement.createSVGTransform();
    groupTransform.setTranslate(this.position.x, this.position.y);
    group.transform.baseVal.appendItem(groupTransform);

    this.nativeElement = group;
    this.nativeTransform = groupTransform;
    this.setupChildren(svg, group);
    svg.nativeElement.appendChild(group);
    return group;
  }

  public getPosition(): Vector {
    return this.position;
  }

  public applyNextVec(): void {
    this.position.addSet(this.nextPosition);
    this.nextPosition = Vector.zero();
    this.nativeTransform?.setTranslate(this.position.x, this.position.y);
    this.positionChanged.emit(this.position);
  }

  public setPositionXY(x: number, y: number) {
    this.position.setXY(x, y);
    this.nativeTransform?.setTranslate(x, y);
    this.positionChanged.emit(this.position);
  }

  public setPosition(position: Vector) {
    this.setPositionXY(position.x, position.y);
  }

  public setHidden(hidden: boolean) {
    if (this.hidden != hidden) {
      this.hidden = hidden;
      if (this.nativeElement) {
        this.nativeElement.setAttribute("visibility", hidden ? "hidden" : "visible");
      }
    }
  }

  public isHidden(): boolean {
    return this.hidden;
  }
}

export class Edge {
  public predicate: string;
  public from: Node;
  public to: NodeBase;
  private hidden: boolean = false;
  protected line?: SVGLineElement;
  protected text?: SVGTextElement;
  protected group?: SVGGElement;
  protected textTransform?: SVGTransform;

  constructor(from: Node, to: NodeBase, predicate: string) {
    this.from = from;
    this.to = to;
    this.predicate = predicate;
    this.from.positionChanged.subscribe(this.handleFromChanged);
    this.to.positionChanged.subscribe(this.handleToChanged);
  }

  public setup(svg: ElementRef<SVGSVGElement>): SVGGElement {
    this.group = document.createElementNS(svgNs, "g") as SVGGElement;

    this.line = document.createElementNS(svgNs, "line") as SVGLineElement;
    this.line.setAttribute("stroke", "black");
    this.line.setAttribute("stroke-width", "2");
    this.line.setAttribute("x1", this.from.getPosition().x.toString());
    this.line.setAttribute("y1", this.from.getPosition().y.toString());
    this.line.setAttribute("x2", this.to.getPosition().x.toString());
    this.line.setAttribute("y2", this.to.getPosition().y.toString());

    const textPos = this.from.getPosition().add(this.to.getPosition()).mulSet(0.5);
    this.text = document.createElementNS(svgNs, "text") as SVGTextElement;
    this.text.textContent = this.predicate;
    this.text.setAttribute("dominant-baseline", "middle");
    this.text.setAttribute("x", textPos.x.toString());
    this.text.setAttribute("y", textPos.y.toString());
    this.textTransform = svg.nativeElement.createSVGTransform();
    this.text.transform.baseVal.appendItem(this.textTransform);

    this.group.appendChild(this.line);
    this.group.appendChild(this.text);
    return this.group;
  }

  private updateTextPos() {
    const textPos = this.from.getPosition().add(this.to.getPosition()).mulSet(0.5);
    this.text?.setAttribute("x", textPos.x.toString());
    this.text?.setAttribute("y", textPos.y.toString());
    const angle = Math.atan2(textPos.y, textPos.x) * (180 / Math.PI);
    this.textTransform?.setRotate(angle, textPos.x, textPos.y);
  }

  private handleFromChanged(newPos: Vector) {
    this.line?.setAttribute("x1", newPos.x.toString());
    this.line?.setAttribute("y1", newPos.y.toString());
    this.updateTextPos();
  }

  private handleToChanged(newPos: Vector) {
    this.line?.setAttribute("x2", newPos.x.toString());
    this.line?.setAttribute("y2", newPos.y.toString());
    this.updateTextPos();
  }

  public setHidden(hidden: boolean) {
    if (this.hidden != hidden) {
      this.hidden = hidden;
      if (this.group) {
        this.group.setAttribute("visibility", hidden ? "hidden" : "visible");
      }
    }
  }

  public isHidden(): boolean {
    return this.hidden;
  }

}

export class LiteralNode extends NodeBase {
  
  public content: string;

  override setupChildren(svg: ElementRef<SVGSVGElement>, group: SVGGElement): void {
  }

  public override getRadius(): number {
    return nodeRadius;
  }

  constructor(content: string, color: string, position: Vector) {
    super(content, color, position);
    this.content = content;
  }
}

const SPLIT_PATTERN = new RegExp("\/#");
export class Node extends NodeBase {
  public iri: string;
  public leafs: Map<Edge, LiteralNode> = new Map<Edge, LiteralNode>();
  public leafsHidden: boolean = false;
  private leafRadius: number = 0;

  constructor(iri: string, color: string, position?: Vector) {
    const label = iri.split(SPLIT_PATTERN).slice(-2).join("/");
    super(label, color, position);
    this.iri = iri;
  }

  public override getRadius(): number {
    return this.leafsHidden ? nodeRadius : this.leafRadius;
  }

  override setupChildren(svg: ElementRef<SVGSVGElement>, group: SVGGElement): void {
    this.leafs.forEach((leaf, edge) => {
      group.appendChild(edge.setup(svg));
      group.appendChild(leaf.setup(svg));
    });
  }

  private getMaxLeafRadius(): number {
    let max = this.leafRadius;
    this.leafs.forEach((leaf) => {
      const dist = leaf.getPosition().sub(this.position).length();
      if (max < dist)
        max = dist;
    });

    return max;
  }

  public updateLeafRadius() {
    const maxRad = this.getMaxLeafRadius();
    const angles = Math.max(7, this.leafs.size);
    const minCircumference = angles * nodeRadius * 2;
    this.leafRadius = Math.max(maxRad, minCircumference / (2 * Math.PI));
  }

  public relayoutLeafes(): void {
    this.updateLeafRadius();
    const angles = Math.max(7, this.leafs.size);
    let angleCounter = 0;
    const angleSize = 2 * Math.PI / angles;
    this.leafs.forEach((leaf, edge) => {
      const angle = angleCounter++ * angleSize;
      leaf.setPositionXY(this.leafRadius * Math.cos(angle), this.leafRadius * Math.sin(angle));
    });
  }

  private handleLeadUpdate(newPos: Vector) {
    this.leafRadius = Math.max(this.leafRadius, newPos.sub(this.position).length());
  }

  public addLiteral(predicate: string, literal: string): void {
    const node = new LiteralNode(literal, this.color, this.position);
    node.positionChanged.subscribe(this.handleLeadUpdate);
    const edge = new Edge(this, node, predicate);
    this.leafs.set(edge, node);
  }

}
