import { AfterViewInit, Component, ElementRef, Input, ViewChild } from '@angular/core';
import { Graph } from '../graph/graph';
import { NaiveGraphLayout } from '../graph/naive-layout';
import { IGraphLayout } from '../graph/graph-layout';
import { NgFor, NgIf } from '@angular/common';
import { Edge } from '../graph/edge';

@Component({
  selector: 'graph',
  imports: [NgFor, NgIf],
  standalone: true,
  templateUrl: './graph-visualisation.component.html',
  styleUrl: './graph-visualisation.component.css'
})
export class GraphVisualisationComponent implements AfterViewInit {

  @Input() graph!: Graph;
  private _layouting: IGraphLayout;
  viewBox: string = "0 0 1000 1000";

  graphReady: boolean = false;

  getTextTransform(edge: Edge) {
    const midX = (edge.getFrom().x + edge.getTo().x) / 2;
    const midY = (edge.getFrom().y + edge.getTo().y) / 2;
    const angle = Math.atan2(edge.getTo().y - edge.getFrom().y, edge.getTo().x - edge.getFrom().x) * (180 / Math.PI);
    return `rotate(${angle}, ${midX}, ${midY})`;
  }

  constructor() {
    this._layouting = new NaiveGraphLayout();
  }

  ngAfterViewInit() {
    if(this.graph){
      const bbox = this._layouting.layout(this.graph, 1000);
      this.viewBox = `${bbox.x} ${bbox.y} ${bbox.width} ${bbox.height}`;
      this.graphReady = true;
    }
  } 
}
