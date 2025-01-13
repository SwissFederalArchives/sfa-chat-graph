import { Node } from './node';

export class Edge {
  shouldRender(): boolean {
    return this.getTo().shouldRender() && this.getFrom().shouldRender();
  }

  getTo(): Node {
    return this.toNode!;
   }

  getFrom(): Node {
    return this.fromNode!;
  }

  public fromNode?: Node;
  public toNode?: Node;

  getOther(node: Node): Node | undefined {
    return node === this.fromNode ? this.toNode : this.fromNode;
  }

  constructor(
    public id: string,
    public label: string,
    public source: string,
    public target: string,
    public color: string,
  ) { }
}
