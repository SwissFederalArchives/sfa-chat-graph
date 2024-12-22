import { Edge } from './edge';
import { Node } from './node';

export class Graph {


  private _nodes: Map<string, Node> = new Map();
  private _edges: Map<string, Edge> = new Map();

  getEdges() {
    return Array.from(this._edges.values());
  }

  getOrCreateNode(id: string, label?: string, color?: string) {
    let node = this.getNode(id);
    if (!node) {
      node = this.createNode(id, label, color);
    } 
    
    return node;
  }

  createTripleLiteralObj(subIri: string, predIri: string, obj: string) {
    const node1 = this.getOrCreateNode(subIri);
    const node2 = this.createNode(`${subIri}@${predIri}=${obj}`, obj, "#AF9030");
    this.createEdge(node1.id, node2.id, predIri, predIri);
  }
  
  createTriple(subIri: string, predIri: string, objIri: string) {
    const node1 = this.getOrCreateNode(subIri);
    const node2 = this.getOrCreateNode(objIri);
    this.createEdge(node1.id, node2.id, predIri, predIri);
  }

  insertNode(node: Node): void {
    this._nodes.set(node.id, node);
  }

  createNode(id: string, label?: string, color?: string): Node {
    const node = new Node(id, label ?? id, 0, 0, 40, color ?? "#AF3090");
    this.insertNode(node);
    return node;
  }

  remove(node: Node): void {
    this._nodes.delete(node.id);
  }

  getNodes(): Node[] {
    return Array.from(this._nodes.values());
  }

  getNode(id: string): Node | undefined {
    return this._nodes.get(id);
  }

  private makeEdgeId(edge: Edge): string {
    return `${edge.source}-${edge.id}-${edge.target}`;
  }

  insertEdge(edge: Edge): void {
    this._edges.set(this.makeEdgeId(edge), edge);
  }

  createEdge(fromId: string, toId: string, edgeId: string, label: string): void {
    const edge = new Edge(edgeId, label, fromId, toId, "#101010");
    this.insertEdge(edge);
  }

  updateModels(): void {
    for (const node of this._nodes.values()) {
      node.edges.length = 0;
    }

    for (const edge of this._edges.values()) {
      edge.fromNode = this._nodes.get(edge.source);
      edge.toNode = this._nodes.get(edge.target);
      edge.fromNode?.edges?.push(edge);
      edge.toNode?.edges?.push(edge);
    }
  }
}
