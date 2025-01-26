import { EventEmitter } from '@angular/core';
import { Edge } from './edge';
import { Node } from './node';
import { Vector } from './vector';
import { firstValueFrom } from 'rxjs';
import { AwaitableEventEmitter } from '../utils/awaitable-event-emitter';
import { SparqlStarResult } from '../services/api-client/sparql-star-result.model';
import { SubGraph } from './sub-graph';

export class Graph {

  private _nodes: Map<string, Node> = new Map();
  private _edges: Map<string, Edge> = new Map();
  private _adjacencies: Map<[Node, Node], Edge> = new Map();
  private _subGraphs: Map<string, SubGraph> = new Map();

  public readonly onNodeDetailsRequested: AwaitableEventEmitter<{ graph: Graph, node: Node }, unknown> = new AwaitableEventEmitter<{ graph: Graph, node: Node }, unknown>(true);
  public readonly onLeafNodesLoaded: EventEmitter<Node> = new EventEmitter<Node>();
  public readonly onNodeCreated: EventEmitter<Node> = new EventEmitter<Node>();
  public readonly onEdgeCreated: EventEmitter<Edge> = new EventEmitter<Edge>();
  public readonly onGraphUpdated: EventEmitter<Graph> = new EventEmitter<Graph>();

  async loadLeafs(node: Node): Promise<void> {
    if (node.areLeafsLoaded() == false) {
      await this.onNodeDetailsRequested.emitAsync({ graph: this, node: node })
      node.markLeafsLoaded();
      this.updateModels();
      this.onLeafNodesLoaded.emit(node);
    }
  }

  public getSubGraphs(): SubGraph[] {
    return Array.from(this._subGraphs.values());
  }

  public getSubGraph(id: string): SubGraph | undefined {
    return this._subGraphs.get(id);
  }

  loadFromSparqlStar(sparqlStar: SparqlStarResult, maxVisisbleChildren: number = 20, subGraphId?: string, headerVars: string[] = ['s', 'p', 'o']): void {
    let childCount = 0;
    for (var key in sparqlStar.results.bindings.sort((a: any, b: any) => a[headerVars[2]].type.localeCompare(b[headerVars[2]].type))) {
      const item = sparqlStar.results.bindings[key];
      const sub = item[headerVars[0]].value;
      const pred = item[headerVars[1]].value;
      const obj = item[headerVars[2]].value;
      if (item[headerVars[2]].type == "uri") {
        const created = this.createTriple(sub, pred, obj, subGraphId);
        if (childCount++ > maxVisisbleChildren) {
          if (created.subCreated)
            created.sub.setHidden(true);

          if (created.objCreated)
            created.obj.setHidden(true);
        }
      } else {
        const created = this.createTripleLiteralObj(sub, pred, obj, subGraphId);
        if (childCount++ > maxVisisbleChildren) {
          if (created.subCreated)
            created.sub.setHidden(true);

          if (created.objCreated || created.sub.isHidden())
            created.obj.setHidden(true);
        }
      }
    }
  }

  getEdges() {
    return Array.from(this._edges.values());
  }

  getOrCreateSubGraph(id?: string): SubGraph | undefined {
    if (id == undefined)
      return undefined;

    if (this._subGraphs.has(id) == false) {
      const hue = Math.floor(Math.random() * 360);
      const nodeColor = `hsl(${hue}, 100%, 70%)`;
      const leafColor = `hsl(${hue}, 100%, 50%)`;
      const subGraph = new SubGraph(id, nodeColor, leafColor);
      this._subGraphs.set(id, subGraph);
      return subGraph;
    } else {
      return this._subGraphs.get(id)!;
    }
  }

  getOrCreateNode(id: string, iri: string, label?: string, subGraphId?: string, isLeaf: boolean = false): { node: Node, created: boolean } {
    let node = this.getNode(id);
    let created = false;
    if (!node) {
      node = this.createNode(id, iri, label, subGraphId, isLeaf);
      created = true;
    }

    return { node: node, created: created };
  }

  readonly splitExp: RegExp = new RegExp("\\/#");
  createTripleLiteralObj(subIri: string, predIri: string, obj: string, subGraphId?: string): { sub: Node, obj: Node, subCreated: boolean, objCreated: boolean } {
    const node1 = this.getOrCreateNode(subIri, subIri, subIri.split("/").slice(-2).join("/"), subGraphId);
    const node2 = this.getOrCreateNode(`LITERAL(${subIri}@${predIri})`, obj, obj, subGraphId, true);
    this.getOrCreateEdge(node1.node.id, node2.node.id, predIri, predIri.split("#").slice(-1).join("/"));
    return { sub: node1.node, subCreated: node1.created, obj: node2.node, objCreated: node2.created };
  }

  createTriple(subIri: string, predIri: string, objIri: string, subGraphId?: string): { sub: Node, subCreated: boolean, obj: Node, objCreated: boolean } {
    const node1 = this.getOrCreateNode(subIri, subIri, subIri.split("/").slice(-2).join("/"), subGraphId);
    const node2 = this.getOrCreateNode(objIri, objIri, objIri.split("/").slice(-2).join("/"), subGraphId);
    this.getOrCreateEdge(node1.node.id, node2.node.id, predIri, predIri.split("#").slice(-1).join("/"));
    return { sub: node1.node, subCreated: node1.created, obj: node2.node, objCreated: node2.created };
  }

  isAdjacent(node1: Node, node2: Node): boolean {
    return node1.edges.some((edge, _) => edge.getOther(node1) == node2);
  }

  insertNode(node: Node): void {
    this._nodes.set(node.id, node);
  }

  createNode(id: string, iri: string, label?: string, subGraphId?: string, isLeaf: boolean = false): Node {
    const subGraph = this.getOrCreateSubGraph(subGraphId ?? "default");
    const color = isLeaf ? (subGraph?.leafColor ?? "#CFA060") : (subGraph?.nodeColor ?? "#CF60A0");
    const node = new Node(id, iri, label ?? id, Vector.zero(), 40, color, subGraph, false, !isLeaf);
    subGraph?.nodes.push(node);
    this.insertNode(node);
    this.onNodeCreated.emit(node);
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

  // private insertEdge(edge: Edge): void {
  //   this._edges.set(edge.id, edge);
  //   this._adjacencies.set([edge.fromNode!, edge.toNode!], edge);
  //   this._adjacencies.set([edge.toNode!, edge.fromNode!], edge);
  // }

  getOrCreateEdge(fromId: string, toId: string, edgeId: string, label: string): Edge {
    const id = `${fromId}-${edgeId}-${toId}`;
    if (this._edges.has(id) == false) {
      const edge = new Edge(id, edgeId, label, fromId, toId, "#101010");
      this.onEdgeCreated.emit(edge);
      this._edges.set(id, edge);
      return edge;
    } else {
      return this._edges.get(id)!;
    }
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

    this._nodes.forEach(node => {
      if (node.subGraph) {
        node.color = node.isLeaf() ? node.subGraph.leafColor : node.subGraph.nodeColor;
      }
    });

    this.onGraphUpdated.emit(this);
  }
}
