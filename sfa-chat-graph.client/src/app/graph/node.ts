import { Edge } from "./edge";

export class Node {
  public edges: Edge[] = []

  constructor(
    public id: string,
    public label: string,
    public x: number,
    public y: number,
    public radius: number,
    public color: string,
  ) { }

  move(x: number, y: number): void {
    const deltaX = x - this.x;
    const deltaY = y - this.y;
    this.x = x;
    this.y = y;

    if (this.isLeaf() == false) {
      this.getLeafNodes().forEach(leaf => {
        leaf.x += deltaX;
        leaf.y += deltaY;
      });
    }
  }

  getLeafNodes(): Node[] {
    return this.edges.map(edge => edge.getOther(this)!).filter(other => other.isLeaf());
  }

  isLeaf(): boolean {
    return this.edges.length <= 1;
  }
}
