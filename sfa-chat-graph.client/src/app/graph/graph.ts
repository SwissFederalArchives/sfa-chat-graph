import { EventEmitter } from '@angular/core';
import { Edge } from './edge';
import { Node } from './node';
import { Vector } from './vector';

export class Graph {

  private _nodes: Map<string, Node> = new Map();
  private _edges: Map<string, Edge> = new Map();
  private _adjacencies: Map<[Node, Node], Edge> = new Map();
  public readonly onNodeDetailsRequested: EventEmitter<{ graph: Graph, node: Node }> = new EventEmitter<{ graph: Graph, node: Node }>();
  public readonly onLeafNodesLoaded: EventEmitter<Node> = new EventEmitter<Node>();

  loadLeafes(node: Node): void {
    if (node.areLeafesLoaded() == false) {
      this.onNodeDetailsRequested.emit({ graph: this, node: node });
      node.markLeafesLoaded();
      this.updateModels();
      this.onLeafNodesLoaded.emit(node);
    }
  }

  getEdges() {
    return Array.from(this._edges.values());
  }

  getOrCreateNode(id: string, label?: string, color?: string): { node: Node, created: boolean } {
    let node = this.getNode(id);
    let created = false;
    if (!node) {
      node = this.createNode(id, label, color);
      created = true;
    }

    return {node: node, created: created};
  }

  readonly splitExp: RegExp = new RegExp("\\/#");
  createTripleLiteralObj(subIri: string, predIri: string, obj: string): { sub: Node, obj: Node, subCreated: boolean, objCreated: boolean } {
    const node1 = this.getOrCreateNode(subIri, subIri.split("/").slice(-2).join("/"));
    const node2 = this.createNode(`${subIri}@${predIri}=${obj}`, obj, "#CFA060");
    this.createEdge(node1.node.id, node2.id, predIri, predIri.split("#").slice(-1).join("/"));
    return { sub: node1.node, subCreated: node1.created, obj: node2, objCreated: true };
  }

  createTriple(subIri: string, predIri: string, objIri: string): { sub: Node, subCreated: boolean, obj: Node, objCreated: boolean } {
    const node1 = this.getOrCreateNode(subIri, subIri.split("/").slice(-2).join("/"));
    const node2 = this.getOrCreateNode(objIri, objIri.split("/").slice(-2).join("/"));
    this.createEdge(node1.node.id, node2.node.id, predIri, predIri.split("#").slice(-1).join("/"));
    return { sub: node1.node, subCreated: node1.created, obj: node2.node, objCreated: node2.created };
  }

  isAdjacant(node1: Node, node2: Node): boolean {
    return node1.edges.some((edge, _) => edge.getOther(node1) == node2);
  }

  insertNode(node: Node): void {
    this._nodes.set(node.id, node);
  }

  createNode(id: string, label?: string, color?: string): Node {
    const node = new Node(id, label ?? id, Vector.zero(), 40, color ?? "#CF60A0");
    this.insertNode(node);
    return node;
  }

  remove(node: Node): void {
    this._nodes.delete(node.id);
  }

  getNodes(): Node[] {
    return Array.from(this._nodes.values());
  }

  getCenterNodes(): Node[] {
    return this.getNodes().filter(node => node.isLeaf() == false)
  }

  getNode(id: string): Node | undefined {
    return this._nodes.get(id);
  }

  private makeEdgeId(edge: Edge): string {
    return `${edge.source}-${edge.id}-${edge.target}`;
  }

  insertEdge(edge: Edge): void {
    this._edges.set(this.makeEdgeId(edge), edge);
    this._adjacencies.set([edge.fromNode!, edge.toNode!], edge);
    this._adjacencies.set([edge.toNode!, edge.fromNode!], edge);
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
