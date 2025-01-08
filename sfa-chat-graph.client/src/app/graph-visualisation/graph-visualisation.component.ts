import { AfterViewInit, Component, ElementRef, HostListener, Input, ViewChild } from '@angular/core';
import { Graph } from '../graph/graph';
import { NaiveGraphLayout } from '../graph/naive-layout';
import { BBox, IGraphLayout } from '../graph/graph-layout';
import { NgFor, NgIf } from '@angular/common';
import { Edge } from '../graph/edge';
import { Node } from '../graph/node';
import { interval, take } from 'rxjs';

@Component({
  selector: 'graph',
  imports: [NgFor, NgIf],
  standalone: true,
  templateUrl: './graph-visualisation.component.html',
  styleUrl: './graph-visualisation.component.css'
})

export class GraphVisualisationComponent implements AfterViewInit {

  @Input() graph!: Graph;
  @Input() showDebug: boolean = false;
 
  private _layouting!: IGraphLayout;
  private _bbox!: BBox;
  private _layoutTimer: any;
  isGraphStable: boolean = false;

  getViewBox(): string {
    return `${this._bbox.x} ${this._bbox.y} ${this._bbox.width} ${this._bbox.height}`;
  }

  graphReady: boolean = false;

  getTextTransform(edge: Edge) {
    const midX = (edge.getFrom().x + edge.getTo().x) / 2;
    const midY = (edge.getFrom().y + edge.getTo().y) / 2;
    const angle = Math.atan2(edge.getTo().y - edge.getFrom().y, edge.getTo().x - edge.getFrom().x) * (180 / Math.PI);
    return `rotate(${angle}, ${midX}, ${midY})`;
  }

  private draggedNode?: Node;
  private dragLeafNodes: boolean = false;

  onMouseDown(event: MouseEvent, node: any): void {
    this.draggedNode = node;
    event.preventDefault();
    this.stopLayoutTimer();
    this.dragLeafNodes = event.button == 0;
    this.isGraphStable = false
  }

  onMouseMove(event: MouseEvent): void {
    if (this.draggedNode) {
      const svg = (event.target as SVGElement).closest('svg') as SVGSVGElement;
      const pt = svg.createSVGPoint();
      pt.x = event.clientX;
      pt.y = event.clientY;
      const svgCoords = pt.matrixTransform(svg.getScreenCTM()?.inverse() || new DOMMatrix());

      const dx = svgCoords.x - this.draggedNode.x;
      const dy = svgCoords.y - this.draggedNode.y;

      this.draggedNode.x += dx;
      this.draggedNode.y += dy;

      if (this.dragLeafNodes){
        this.draggedNode.getLeafNodes().forEach(leaf => {
          leaf.x += dx;
          leaf.y += dy;
        })
      }

      this._layouting.notifyGraphUpdated();
      this._bbox = this._layouting.getMinimalBbox();
    }
  }

  onMouseUp(event: MouseEvent): void {
    if (this.draggedNode) {
      this.draggedNode = undefined;
      this.startLayoutTimer();
    }
  }

  startLayoutTimer() {
    if (!this._layoutTimer) {
      this._layoutTimer = setInterval(() => {
        const energy = this._layouting.layout(5, 0.05);
        this._bbox = this._layouting.getMinimalBbox();

        if (energy < 0.5){
          this.stopLayoutTimer();
          this.isGraphStable = true;
        }

      }, 25);
    }
  }

  startFadeTimer(){

  }

  stopLayoutTimer() {
    if (this._layoutTimer) {
      clearInterval(this._layoutTimer);
      this._layoutTimer = undefined;
    }
  }

  ngAfterViewInit() {
    if (this.graph) {
      this._layouting = new NaiveGraphLayout(this.graph);
      this._layouting.layout(1, 1);
      this._bbox = this._layouting.getMinimalBbox();
      this.graphReady = true;
      this.startLayoutTimer();
    }
  }
}
