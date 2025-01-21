import { BBox } from "./bbox";
import { Graph } from "./graph";
import { IGraphLayout } from "./graph-layout";
import { Node } from "./node";
import { Vector } from "./vector";




const NODE_CIRCLE_PADDING = 25;

class NodeCircle {
  public readonly minRadius: number = 200;
  public readonly leafPadding: number = 50;
  public readonly node: Node;
  public radius: number;
  public readonly adjacent: NodeCircle[] = [];

  public readonly center: Vector;
  public readonly next: Vector = Vector.zero();

  constructor(centerNode: Node, radius: number, position: Vector) {
    this.node = centerNode;
    this.radius = radius;
    this.center = position;
    this.node.onChanged.subscribe(x => this.notifyNodeUpdated());
  }

  applyVector() {
    if (this.next) {
      this.center.addSet(this.next);
      this.next.clear();
    }
  }

  private rotateX(angle: number, radius: number): number {
    return radius * Math.cos(angle);
  }

  private rotateY(angle: number, radius: number): number {
    return radius * Math.sin(angle);
  }

  relayoutLeafs(){
    const leafs = Array.from(this.node.getVisibleLeafs());
    const radius = leafs.length == 0 ? this.node.radius * 2 : Math.max(this.minRadius, this.radius - NODE_CIRCLE_PADDING, leafs.reduce((sum, current) => sum + this.leafPadding + current.radius * 2, 0) / (2 * Math.PI));
    this.radius = radius;
    this.node.circleRadius = radius;
    leafs.forEach((leaf, index) => {
      const angle = (index / Math.max(7, leafs.length)) * 2 * Math.PI + Math.PI / 3;
      leaf.move(this.node.pos.x + this.rotateX(angle, radius - leaf.radius), this.node.pos.y + this.rotateY(angle, radius - leaf.radius));
    });
  }

  updateNodes() {
    this.node.moveWithLeafs(this.center.x, this.center.y);
  }

  notifyNodeUpdated(): void {
    const leafs = Array.from(this.node.getVisibleLeafs());
    this.radius = leafs.length == 0 ? this.node.radius * 2 : Math.max(this.node.radius * 2, NODE_CIRCLE_PADDING + leafs.map(leaf => this.center.distance(leaf.pos) + leaf.radius).reduce((max, current) => Math.max(max, current), 0));
    this.node.circleRadius = this.radius;
    this.center.setCopy(this.node.pos);
  }
}


class Spring {
  readonly springLength: number = 3;
  readonly springStiffness: number = 0.25;
  readonly forceScale: number = 2;
  readonly distanceForceLimitingDivider: number = 1;

  public circle1: NodeCircle;
  public circle2: NodeCircle;

  constructor(circle1: NodeCircle, circle2: NodeCircle) {
    this.circle1 = circle1;
    this.circle2 = circle2;
  }

  applyForces() {
    if(this.circle1.node.shouldRender() == false || this.circle2.node.shouldRender() == false)
      return;

    const distance = Math.max(0.1, this.circle1.center.distance(this.circle2.center) - this.circle1.radius - this.circle2.radius);
    const force = this.springLength * Math.log(distance / this.springStiffness);
    const vector = this.circle1.center.sub(this.circle2.center).normalize().mul(Math.min(force * this.forceScale, distance / this.distanceForceLimitingDivider));
    this.circle2.next.set(vector)
   // this.circle2.node.debugVectors.push(vector)
    this.circle1.next.set(vector).mulSet(-1);
   // this.circle1.node.debugVectors.push(vector.mul(-1)); 
  }
}

export class NaiveGraphLayout implements IGraphLayout {

  readonly repulsionFactor: number = 300;
  readonly maxRepulsion: number = 150;
  readonly centerAttraction: number = 0.05;
  readonly maxDistance: number = 2000;

  readonly graph: Graph;
  readonly springs: Spring[] = [];
  readonly circleMap: Map<Node, NodeCircle> = new Map<Node, NodeCircle>();
  private nodeCircles: NodeCircle[];

  constructor(graph: Graph) {
    this.graph = graph;
    this.nodeCircles = [];
    this.updateCircles();

    this.graph.onLeafNodesLoaded.subscribe(node => {
      this.relayoutLeafes(node)
    });

    this.graph.onGraphUpdated.subscribe(() => {
      this.updateCircles();
    });
  }

  updateCircles() {
    const newNodes = this.graph.getNodes().filter(node => node.isHidden() == false && node.isLeaf() == false && this.circleMap.has(node) == false);
    newNodes.forEach(node => {
      const leafes = node.getVisibleLeafs();
      const circle = new NodeCircle(node, node.radius * 2 + NODE_CIRCLE_PADDING, Vector.random(4000, leafes.length * 200, 4000 - Math.max(0, (10 - leafes.length) * 200)));
      node.circleRadius = circle.radius;
      this.nodeCircles.push(circle);
      this.circleMap.set(node, circle);
      circle.relayoutLeafs();
      circle.updateNodes();
    });

    const visited = new Set<Node>();
    newNodes.forEach(node => {
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
    });
  }

  relayoutLeafes(node: Node): void {
    if(this.circleMap.has(node)){
      this.circleMap.get(node)!.relayoutLeafs();
    }
  }

  applyRepulsion(circles: NodeCircle[]) {
    for (let i = 0; i < circles.length - 1; i++) {
      for (let j = i + 1; j < circles.length; j++) {
        const circle1 = circles[i];
        const circle2 = circles[j];
        if (circle1.node.shouldRender() == false || circle2.node.shouldRender() == false)
          continue;

        const distance = Math.max(0.1, circle1.center.distance(circle2.center) - circle1.radius - circle2.radius);
        if (distance < this.maxDistance) {
          const force = (this.repulsionFactor * this.repulsionFactor) / (distance * distance);
          const vector = circle1.center.sub(circle2.center).normalize().mul(Math.min(force, this.maxRepulsion));
          circle1.next.addSet(vector);
         // circle1.node.debugVectors.push(vector.copy());
          circle2.next.addSet(vector.mulSet(-1));
         // circle2.node.debugVectors.push(vector);
        }
      }
    }
  }

  applyCenterAttraction(circles: NodeCircle[], center: Vector) {
    circles.filter(x => x.node.shouldRender()).forEach(circle => {
      const distance = Math.max(0.1, circle.center.distance(center) - circle.radius);
      const force = this.centerAttraction * distance;
      const vector = center.sub(circle.center).normalize().mul(Math.min(force, distance / 2));
      circle.next.addSet(vector);
     // circle.node.debugVectors.push(vector);
    })
  }



  layout(steps: number, scale: number = 1): number {
    const center: Vector = Vector.zero();
    const renderingCircles = this.nodeCircles.filter(circle => circle.node.shouldRender());
    let internalEnergy: number = 0;
    for (let i = 0; i < steps; i++) {
      //this.nodeCircles.forEach(circle => circle.node.debugVectors.length = 0);
      this.springs.forEach(spring => spring.applyForces());
      this.applyCenterAttraction(this.nodeCircles, center);
      this.applyRepulsion(this.nodeCircles);
      renderingCircles.forEach(circle => {
        circle.next.mulSet(scale);
        internalEnergy += circle.next.length();
        circle.applyVector()
      });
    }

    this.nodeCircles.forEach(circle => circle.updateNodes());
    return internalEnergy / (renderingCircles.length / 3.0);
  }

  notifyGraphUpdated(): void {
    this.nodeCircles.forEach(circle => circle.notifyNodeUpdated())
  }

  getMinimalBbox(): BBox {
    const renderingNodes = this.graph.getNodes().filter(node => node.shouldRender());
    const minX = renderingNodes.map(node => node.pos.x - node.radius).reduce((min, current) => Math.min(min, current), Number.MAX_VALUE);
    const minY = renderingNodes.map(node => node.pos.y - node.radius).reduce((min, current) => Math.min(min, current), Number.MAX_VALUE);
    const maxX = renderingNodes.map(node => node.pos.x + node.radius).reduce((max, current) => Math.max(max, current), Number.MIN_VALUE);
    const maxY = renderingNodes.map(node => node.pos.y + node.radius).reduce((max, current) => Math.max(max, current), Number.MIN_VALUE);

    return new BBox(minX, minY, maxX - minX, maxY - minY);
  }


}
