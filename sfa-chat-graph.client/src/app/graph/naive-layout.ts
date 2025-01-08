import { Graph } from "./graph";
import { BBox, IGraphLayout } from "./graph-layout";
import { Node } from "./node";


class Vector {
  public x: number = 0;
  public y: number = 0;

  constructor(x: number, y: number) {
    this.x = x;
    this.y = y;
  }

  add(other: Vector): Vector {
    return new Vector(this.x + other.x, this.y + other.y);
  }

  sub(other: Vector): Vector {
    return new Vector(this.x - other.x, this.y - other.y);
  }

  mul(factor: number): Vector {
    return new Vector(this.x * factor, this.y * factor);
  }

  div(factor: number): Vector {
    return new Vector(this.x / factor, this.y / factor);
  }

  length(): number {
    return Math.sqrt(this.x * this.x + this.y * this.y);
  }

  normalize(): Vector {
    return this.div(this.length());
  }

  mulSet(factor: number): Vector {
    this.x *= factor;
    this.y *= factor;
    return this;
  }

  addSet(vec: Vector): Vector {
    this.x += vec.x;
    this.y += vec.y;
    return this;
  }

  set(vec: Vector): Vector {
    this.x = vec.x;
    this.y = vec.y;
    return this;
  }

  setXY(x: number, y: number): Vector {
    this.x = x;
    this.y = y;
    return this;
  }

  distance(other: Vector): number {
    const dx = this.x - other.x;
    const dy = this.y - other.y;
    return Math.sqrt(dx * dx + dy * dy);
  }

  distanceXY(x: number, y: number){
    const dx = this.x - x;
    const dy = this.y - y;
    return Math.sqrt(dx * dx + dy * dy);
  }

  clear(): Vector {
    this.x = 0;
    this.y = 0;
    return this;
  }

  static zero(): Vector {
    return new Vector(0, 0);
  }

  static random(scale: number = 1): Vector {
    return new Vector((Math.random() - 0.5) * scale, (Math.random() - 0.5) * scale);
  }
}

class NodeCircle {

  public readonly node: Node;
  public radius: number;
  public readonly adjacent: NodeCircle[] = [];

  public readonly center: Vector;
  public readonly next: Vector = Vector.zero();

  constructor(centerNode: Node, radius: number, position: Vector) {
    this.node = centerNode;
    this.radius = radius;
    this.center = position;
  }

  applyVector() {
    if (this.next) {
      this.center.addSet(this.next);
      this.next.clear();
    }
  }

  updateNodes() {
    const dx = this.center.x - this.node.x;
    const dy = this.center.y - this.node.y;

    this.node.x += dx;
    this.node.y += dy;

    this.node.getLeafNodes().forEach(leaf => {
      leaf.x += dx;
      leaf.y += dy;
    });
  }

  notifyNodeUpdated(): void {
    this.radius = this.node.getLeafNodes().map(leaf => this.center.distanceXY(leaf.x, leaf.y) + leaf.radius).reduce((max, current) => Math.max(max, current), 0);
    this.node.circleRadius = this.radius;
    this.center.setXY(this.node.x, this.node.y);
  }
}


class Spring {
  readonly springLength: number = 3;
  readonly springStiffness: number = 1;
  readonly forceScale: number = 0.5;

  public circle1: NodeCircle;
  public circle2: NodeCircle;

  constructor(circle1: NodeCircle, circle2: NodeCircle) {
    this.circle1 = circle1;
    this.circle2 = circle2;
  }

  applyForces() {
    const distance = Math.max(0.1, this.circle1.center.distance(this.circle2.center) - this.circle1.radius - this.circle2.radius);
    const force = this.springLength * Math.log(distance / this.springStiffness);
    const vector = this.circle1.center.sub(this.circle2.center).normalize().mul(Math.min(force * this.forceScale, distance / 2));
    this.circle2.next.set(vector)
    this.circle1.next.set(vector).mulSet(-1);
  }
}

export class NaiveGraphLayout implements IGraphLayout {

  minRadius: number = 200;
  nodePadding: number = 50;
  readonly repulsionFactor: number = 1.5;
  readonly centerAttraction: number = 0.1;
  readonly maxDistance: number = 2000;

  readonly graph: Graph;
  readonly springs: Spring[] = [];
  readonly circleMap: Map<Node, NodeCircle> = new Map<Node, NodeCircle>();
  readonly nodeCircles: NodeCircle[];

  constructor(graph: Graph) {
    this.graph = graph;

    const centerNodes = graph.getNodes().filter(node => node.isLeaf() == false);
    for (let i = 0; i < centerNodes.length; i++) {
      const node = centerNodes[i];
      const leafes = node.getLeafNodes();
      const radius = leafes.length == 0 ? node.radius * 2 : Math.max(this.minRadius, leafes.reduce((sum, current) => sum + this.nodePadding + current.radius * 2, 0) / (2 * Math.PI));
      node.circleRadius = radius;
      const circle = new NodeCircle(node, radius, Vector.random(1000));

      this.circleMap.set(node, circle);
      leafes.forEach((leaf, index) => {
        const angle = (index / Math.max(7, leafes.length)) * 2 * Math.PI + Math.PI / 3;
        leaf.move(node.x + this.rotateX(angle, radius - leaf.radius), node.y + this.rotateY(angle, radius - leaf.radius));
      });
    }

    this.nodeCircles = Array.from(this.circleMap.values());

    const visited = new Set<Node>();
    for (let i = 0; i < centerNodes.length; i++) {
      const node = centerNodes[i];
      const circle = this.circleMap.get(node)!;
      visited.add(node);
      node.getSiblings().forEach(sibling => {
        if (visited.has(sibling) == false) {
          const siblingCircle = this.circleMap.get(sibling);
          if (siblingCircle) {
            this.springs.push(new Spring(circle, siblingCircle));
            circle.adjacent.push(siblingCircle);
            siblingCircle.adjacent.push(circle);
          }
        }
      });
    }
  }

  private rotateX(angle: number, radius: number): number {
    return radius * Math.cos(angle);
  }

  private rotateY(angle: number, radius: number): number {
    return radius * Math.sin(angle);
  }


  applyRepulsion(circles: NodeCircle[]) {
    for (let i = 0; i < circles.length - 1; i++) {
      for (let j = i + 1; j < circles.length; j++) {
        const circle1 = circles[i];
        const circle2 = circles[j];
        const distance = Math.max(0.1, circle1.center.distance(circle2.center) - circle1.radius - circle2.radius);
        if (distance < this.maxDistance) {
          const force = this.repulsionFactor / (distance * distance);
          const vector = circle1.center.sub(circle2.center).normalize().mul(force);
          circle1.next.addSet(vector);
          circle2.next.addSet(vector.mulSet(-1));
        }
      }
    }
  }

  applyCenterAttraction(circles: NodeCircle[], center: Vector) {
    circles.forEach(circle => {
      const distance = Math.max(0.1, circle.center.distance(center) - circle.radius);
      const force = this.centerAttraction * distance;
      const vector = center.sub(circle.center).normalize().mul(Math.min(force, distance / 2));
      circle.next.addSet(vector);
    })
  }



  layout(steps: number, scale: number = 1): number {
    const center: Vector = Vector.zero();

    const internalEnergy: Vector = Vector.zero();
    for (let i = 0; i < steps; i++) {
      this.springs.forEach(spring => spring.applyForces());
      this.applyCenterAttraction(this.nodeCircles, center);
      this.applyRepulsion(this.nodeCircles);
      this.nodeCircles.forEach(circle => {
        circle.next.mulSet(scale);
        internalEnergy.addSet(circle.next);
        circle.applyVector()
      });
    }

    this.nodeCircles.forEach(circle => circle.updateNodes());
    return internalEnergy.length();
  }

  notifyGraphUpdated(): void {
    this.nodeCircles.forEach(circle => circle.notifyNodeUpdated())
  }

  getMinimalBbox(): BBox {
    const minX = this.graph.getNodes().map(node => node.x - node.radius).reduce((min, current) => Math.min(min, current), Number.MAX_VALUE);
    const minY = this.graph.getNodes().map(node => node.y - node.radius).reduce((min, current) => Math.min(min, current), Number.MAX_VALUE);
    const maxX = this.graph.getNodes().map(node => node.x + node.radius).reduce((max, current) => Math.max(max, current), Number.MIN_VALUE);
    const maxY = this.graph.getNodes().map(node => node.y + node.radius).reduce((max, current) => Math.max(max, current), Number.MIN_VALUE);

    return new BBox(minX, minY, maxX - minX, maxY - minY);
  }


}
