import { Graph } from "./graph";
import { IGraphLayout } from "./graph-layout";
import { Node } from "./node";

class NodeCircle {

  public readonly node: Node;
  public centerX: number = 0;
  public centerY: number = 0;
  public radius: number = 0;

  constructor(node: Node, radius: number = 0) {
    this.node = node;
    this.radius = radius;
  }
}

export class NaiveGraphLayout implements IGraphLayout {

  minRadius: number = 50;
  nodePadding: number = 10;

  private rotateX(angle: number, radius: number): number {
    return radius * Math.cos(angle);
  }

  private rotateY(angle: number, radius: number): number {
    return radius * Math.sin(angle);
  }

  layout(graph: Graph): void {
    const centerNodes = graph.getNodes().filter(node => node.isLeaf() == false);
    const circles: NodeCircle[] = []
    for(let node of centerNodes) {
      const leafes = node.getLeafNodes();
      const radius = Math.max(this.minRadius, leafes.reduce((sum, current) => sum + this.nodePadding + current.radius * 2, 0) / 2 * Math.PI);
      circles.push(new NodeCircle(node, radius));
      leafes.forEach((leaf, index) => {
        const angle = (index / leafes.length) * 2 * Math.PI;
        leaf.move(node.x + this.rotateX(angle, radius - leaf.radius), node.y + this.rotateY(angle, radius - leaf.radius));
      });
    }


  }


}
