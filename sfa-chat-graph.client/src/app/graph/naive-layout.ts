import { Graph } from "./graph";
import { IGraphLayout } from "./graph-layout";
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
  static random(): Vector {
    return new Vector(Math.random(), Math.random());
  }
}


class NodeCircle {

  private readonly REPULSION_DISTANCE = 250;
  private readonly SPRING_SCALE = 0.1;
  public readonly node: Node;
  public centerX: number = 0;
  public centerY: number = 0;
  public radius: number = 0;
  public springs: Map<NodeCircle, number> = new Map();

  private _vec?: Vector;

  applyVector() {
    if (this._vec) {
      this.centerX += this._vec.x;
      this.centerY += this._vec.y;
    }
  }

  setVector(vec: Vector) {
    this._vec = vec;
  }

  getVector(): Vector | undefined {
    return this._vec;
  }

  clearVector() {
    this._vec = undefined;
  }

  updateNode() {
    this.node.moveWithLeafs(this.centerX, this.centerY);
  }

  updateSpringForceTo(other: NodeCircle): Vector {
    if (other === this)
      return new Vector(0, 0);

    const dx = this.centerX < other.centerX ? (this.centerX + this.radius) - (other.centerX - other.radius) : (this.centerX - this.radius) - (other.centerX + other.radius);
    const dy = this.centerY < other.centerY ? (this.centerY + this.radius) - (other.centerY - other.radius) : (this.centerY - this.radius) - (other.centerY + other.radius);

    let result: Vector;

    if (this.springs.has(other)) {
      const attraction = this.springs.get(other)!;
      result = new Vector(dx * attraction * this.SPRING_SCALE, dy * attraction * this.SPRING_SCALE);
    } else {
      const repulsion = 0.01;
      result = new Vector((1.0 / (dx == 0 ? 1 : dx)) * repulsion * this.REPULSION_DISTANCE * this.SPRING_SCALE, (1.0 / (dy == 0 ? 1 : dy)) * repulsion * this.REPULSION_DISTANCE * this.SPRING_SCALE);
    }

    if (this._vec) {
      this._vec = this._vec.add(result);
    } else {
      this._vec = result;
    }

    return result;
  }

  constructor(node: Node, radius: number = 0) {
    this.node = node;
    this.radius = radius;
  }
}

export class NaiveGraphLayout implements IGraphLayout {

  minRadius: number = 200;
  nodePadding: number = 50;

  private rotateX(angle: number, radius: number): number {
    return radius * Math.cos(angle);
  }

  private rotateY(angle: number, radius: number): number {
    return radius * Math.sin(angle);
  }

  layout(graph: Graph): void {

    const centerNodes = graph.getNodes().filter(node => node.isLeaf() == false);
    console.log(centerNodes)
    let circles: Map<Node, NodeCircle> = new Map();
    let currentX = 0;
    let currentY = 0;

    for (let node of centerNodes) {
      const leafes = node.getLeafNodes();
      const radius = Math.max(this.minRadius, leafes.reduce((sum, current) => sum + this.nodePadding + current.radius * 2, 0) / (2 * Math.PI));
      const circle = new NodeCircle(node, radius);
      circle.centerX = currentX;
      circle.centerY = currentY;
      currentX += radius*1.5;
      currentY += 0;

      circles.set(node, circle);
      leafes.forEach((leaf, index) => {
        const angle = (index / Math.max(5, leafes.length)) * 2 * Math.PI + Math.PI / 3;
        leaf.move(node.x + this.rotateX(angle, radius - leaf.radius), node.y + this.rotateY(angle, radius - leaf.radius));
      });
    }
    console.log(circles);

    for (let edge of graph.getEdges()) {
      if (circles.has(edge.getFrom()) && circles.has(edge.getTo())) {
        const fromCircle = circles.get(edge.getFrom())!;
        const toCircle = circles.get(edge.getTo())!;
        fromCircle.springs.set(toCircle, -0.1);
        toCircle.springs.set(fromCircle, -0.1);
      }
    }

    for (let i = 0; i < 0; i++) {
      for (let circle of circles.values()) {
        circle.clearVector();
      }

      for (let circle of circles.values()) {
        for (let other of circles.values()) {
          circle.updateSpringForceTo(other);
        }
      }

      for (let circle of circles.values()) {
        console.log(circle.getVector())
        circle.applyVector();
      }
    }

    circles.forEach((c, n) => c.updateNode());

    let minX = 0, minY = 0;
    graph.getNodes().forEach(x => {
      minX = Math.min(minX, x.x - x.radius);
      minY = Math.min(minY, x.y - x.radius);
    });

    minX *= -1;
    minY *= -1;
    for (let node of graph.getNodes()) {
      node.move(minX, minY);
    }
  }


}
