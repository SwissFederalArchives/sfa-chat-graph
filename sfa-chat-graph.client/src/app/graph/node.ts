import { Edge } from "./edge";

export class Node {
  public edges: Edge[] = []
  public circleRadius: number;

  constructor(
    public id: string,
    public label: string,
    public x: number,
    public y: number,
    public radius: number,
    public color: string,
  ) { 
    this.circleRadius = radius;
  }

  moveWithLeafs(x: number, y: number): void {
    const deltaX = x - this.x;
    const deltaY = y - this.y;
    this.x = x;
    this.y = y;

    if (this.isLeaf() == false) {
      this.getLeafNodes().forEach(leaf => leaf.move(deltaX, deltaY));
    }
  }

  move(x: number, y: number): void {
    this.x += x;
    this.y += y;
  }

  getSiblings(): Node[] {
    return this.edges.map(edge => edge.getOther(this)!).filter(other => other.isLeaf() == false);
  }

  getLeafNodes(): Node[] {
    return this.edges.map(edge => edge.getOther(this)!).filter(other => other.isLeaf());
  }

  isLeaf(): boolean {
    return this.edges.length <= 1;
  }
}
