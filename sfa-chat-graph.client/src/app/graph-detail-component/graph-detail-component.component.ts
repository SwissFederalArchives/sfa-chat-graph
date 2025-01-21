import { Component, EventEmitter, Input, input } from '@angular/core';
import { Graph } from '../graph/graph';
import { Node } from '../graph/node';
import { NgFor, NgIf } from '@angular/common';
import { MatDividerModule } from '@angular/material/divider';
import { MatButtonModule } from '@angular/material/button';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { Edge } from '../graph/edge';
import { GraphVisualisationComponent } from '../graph-visualisation/graph-visualisation.component';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'node-detail',
  imports: [NgFor, NgIf, MatDividerModule, MatButtonModule, MatIcon, FormsModule],
  templateUrl: './graph-detail-component.component.html',
  styleUrl: './graph-detail-component.component.css'
})

export class GraphDetailComponentComponent {


  public selectedNode?: Node
  filter?: string = undefined;

  constructor(private _parent: GraphVisualisationComponent) {

  }

  getEdges(): Iterable<Edge> {
    if(this.filter){
      const regexp = new RegExp(this.filter, 'i');
      return this.selectedNode!.getOutgoingEdges().filter(edge => regexp.test(edge.iri) || regexp.test(edge.getTo().iri));
    }else{
      return this.selectedNode!.getOutgoingEdges();
    }
  }

  hideNode(node: Node) {
    node.setHidden(true);
    if (node.isLeaf() == false || this.selectedNode!.getVisibleLeafs().length == 0) {
      this._parent.startLayoutTimer();
    }

    this._parent.getLayouting().relayoutLeafes(this.selectedNode!);
  }

  showNode(node: Node) {
    node.setHidden(false);
    if (node.isLeaf() == false || this.selectedNode!.getVisibleLeafs().length == 1) {
      this._parent.startLayoutTimer();
    }

    this._parent.getLayouting().relayoutLeafes(this.selectedNode!);
  }

  collapseNode() {
    this.selectedNode!.setCollapsed(true);
    this._parent.startLayoutTimer();
  }

  expandNode() {
    this.selectedNode!.setCollapsed(false);
    this._parent.startLayoutTimer();
  }

  formatObjIri(edge: Edge) {
    const to = edge.getTo();
    const iri = to.iri;
    if (iri.startsWith("https://")) {
      return `<${iri}>`;
    } else {
      return `"${iri}"`;
    }
  }

  download() {
    const ttl = this.selectedNode?.getOutgoingEdges().map(edge => `<${edge.getFrom().id}> <${edge.iri}> ${this.formatObjIri(edge)} .`).join('\n');
    const blob = new Blob([ttl!], { type: 'text/turtle' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = 'node.ttl';
    a.click();
    document.body.removeChild(a);
  }

  setNode(node: Node) {
    this.selectedNode = node;
  }

  close() {
    this.selectedNode = undefined;
  }
}
