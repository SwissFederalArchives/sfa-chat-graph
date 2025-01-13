import { AfterViewInit, Component, ElementRef, EventEmitter, HostListener, INJECTOR, Input, ViewChild } from '@angular/core';
import { Graph } from '../graph/graph';
import { NaiveGraphLayout } from '../graph/naive-layout';
import { IGraphLayout } from '../graph/graph-layout';
import { NgFor, NgIf } from '@angular/common';
import { Edge } from '../graph/edge';
import { Node } from '../graph/node';
import { interval, take } from 'rxjs';
import { Vector } from '../graph/vector';
import { BBox } from '../graph/bbox';
import { GraphVisualisationControlsComponent } from '../graph-visualisation-controls/graph-visualisation-controls.component';

@Component({
  selector: 'graph',
  imports: [NgFor, NgIf, GraphVisualisationControlsComponent],
  standalone: true,
  templateUrl: './graph-visualisation.component.html',
  styleUrl: './graph-visualisation.component.css'
})

export class GraphVisualisationComponent implements AfterViewInit {

  @Input() graph!: Graph;
  @Input() showDebug: boolean = false;
  @ViewChild("canvas") svg!: ElementRef<SVGSVGElement>;

  private _layouting!: IGraphLayout;
  private _bbox!: BBox;
  private _layoutTimer: any;
  private _panOffset: Vector = Vector.zero();
  private _zoomLevel: number = 1;
  private _svgPoint!: DOMPoint;
  private _paused: boolean = false;
  public readonly onCollapsed: EventEmitter<Node> = new EventEmitter<Node>();

  private readonly _defaultDomMatrix: DOMMatrix = new DOMMatrix();

  isGraphStable: boolean = false;
  graphReady: boolean = false;
  lastMousePosition?: Vector = undefined;

  public getLayouting(): IGraphLayout {
    return this._layouting;
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

  isPaused(): boolean {
    return this._paused;
  }

  setPaused(paused: boolean) {
    if (paused != this._paused) {
      this._paused = paused;
      if (paused) {
        this.stopLayoutTimer();
      } else {
        this.startLayoutTimer();
      }
    }
  }

  getTextTransform(edge: Edge) {
    const midX = (edge.getFrom().pos.x + edge.getTo().pos.x) / 2;
    const midY = (edge.getFrom().pos.y + edge.getTo().pos.y) / 2;
    const angle = Math.atan2(edge.getTo().pos.y - edge.getFrom().pos.y, edge.getTo().pos.x - edge.getFrom().pos.x) * (180 / Math.PI);
    return `rotate(${angle}, ${midX}, ${midY})`;
  }

  private draggedNode?: Node;
  private dragLeafNodes: boolean = false;
  private isPan: boolean = false;

  getViewBox(): string {
    return `${(this._bbox.x + this._panOffset.x) * this._zoomLevel} ${((this._bbox.y + this._panOffset.y) * this._zoomLevel)} ${this._bbox.width * this._zoomLevel} ${this._bbox.height * this._zoomLevel}`;
  }


  onRightClick(event: MouseEvent): boolean {
    event.preventDefault();
    return false;
  }


  onScroll(event: WheelEvent) {
    event.preventDefault();
    const delta = this.clamp(event.deltaY, -5, 5) * 0.01;
    this._zoomLevel = Math.min(1, Math.max(0.125, this._zoomLevel + delta));
    // const newZoomLevel = Math.min(1, Math.max(0.125, this._zoomLevel + delta));
    // const zoomFactor = newZoomLevel / this._zoomLevel;
    // this._zoomLevel = newZoomLevel;

    const pt = this.getSvgPoint();
    pt.x = event.clientX;
    pt.y = event.clientY;
    const svgCoords = pt.matrixTransform(this.svg.nativeElement.getScreenCTM()?.inverse() || this._defaultDomMatrix);

    const currentX = svgCoords.x;
    const currentY = svgCoords.y;

    const panX = ((currentX) / this._zoomLevel) - (currentX);
    const panY = ((currentY) / this._zoomLevel) - (currentY);

    this.setPan(panX, panY);
  }

  clamp(value: number, min: number, max: number) {
    return Math.min(max, Math.max(value, min));
  }

  updatePan() {
    const maxPanX = ((this._bbox.width / this._zoomLevel) - this._bbox.width);
    const maxPanY = ((this._bbox.height / this._zoomLevel) - this._bbox.height);
    this._panOffset.setXY(this.clamp(this._panOffset.x, -maxPanX, maxPanX), this.clamp(this._panOffset.y, -maxPanY, maxPanY));
  }

  setPan(x: number, y: number) {
    const maxPanX = ((this._bbox.width / this._zoomLevel) - this._bbox.width);
    const maxPanY = ((this._bbox.height / this._zoomLevel) - this._bbox.height);
    this._panOffset.setXY(this.clamp(x, -maxPanX, maxPanX), this.clamp(y, -maxPanY, maxPanY));
  }

  collapseNode(event: MouseEvent, node: Node) {
    event.preventDefault();
    if (node.areLeafesLoaded()) {
      node.setCollapsed(!node.isCollapsed());
      this.onCollapsed?.emit(node);
    }else{
      this.graph.loadLeafes(node);
    }
  }

  onMouseDown(event: MouseEvent, node: any): void {
    event.preventDefault();
    this.draggedNode = node;
    this.stopLayoutTimer();
    this.dragLeafNodes = event.button == 0;
    this.isGraphStable = false
  }

  getSvgPoint() {
    if (!this._svgPoint) {
      this._svgPoint = this.svg.nativeElement.createSVGPoint();
    }

    return this._svgPoint;
  }

  onMouseMove(event: MouseEvent): void {
    if (this.draggedNode) {
      event.preventDefault();
      const pt = this.getSvgPoint();
      pt.x = event.clientX;
      pt.y = event.clientY;
      const svgCoords = pt.matrixTransform(this.svg.nativeElement.getScreenCTM()?.inverse() || this._defaultDomMatrix);

      const dx = svgCoords.x - this.draggedNode.pos.x;
      const dy = svgCoords.y - this.draggedNode.pos.y;


      if (this.draggedNode.isLeaf()) {
        if (this.dragLeafNodes) {
          const parent = this.draggedNode.getParent();
          if (parent) {
            this.draggedNode.moveRelative(dx, dy);
            const distance = this.draggedNode.pos.distance(parent.pos);
            parent.getLeafNodes().forEach(leaf => {
              if (leaf != this.draggedNode) {
                const vec = leaf.pos.sub(parent.pos).normalizeSet().mulSet(distance);
                leaf.move(vec.x + parent.pos.x, vec.y + parent.pos.y);
              }
            })
          }

        } else {
          this.draggedNode.moveRelative(dx, dy);
        }
      } else {
        if (this.dragLeafNodes) {
          this.draggedNode.moveRelativeWithLeafs(dx, dy);
        } else {
          this.draggedNode.moveRelative(dx, dy);
        }
      }

      this._layouting.notifyGraphUpdated();
      this._bbox = this._layouting.getMinimalBbox();
    } else if (this.isPan) {
      const pt = this.getSvgPoint();
      pt.x = event.clientX;
      pt.y = event.clientY;
      const svgCoords = pt.matrixTransform(this.svg.nativeElement.getScreenCTM()?.inverse() || this._defaultDomMatrix);


      event.preventDefault();
      if (this.lastMousePosition) {
        const dx = this.lastMousePosition.x - svgCoords.x;
        const dy = this.lastMousePosition.y - svgCoords.y;
        this.setPan(this._panOffset.x + dx, this._panOffset.y + dy);
      } else {
        this.lastMousePosition = new Vector(svgCoords.x, svgCoords.y);
      }
    }
  }

  onMouseUp(event: MouseEvent): void {
    if (this.draggedNode) {
      event.preventDefault();
      this.draggedNode = undefined;
      this.startLayoutTimer();
    }

    if (this.isPan) {
      event.preventDefault();
      this.isPan = false;
      this.lastMousePosition = undefined;
    }
  }

  beginPan(event: MouseEvent): void {
    if (!this.draggedNode && this.isPan == false) {
      event.preventDefault();
      this.isPan = true;
    }
  }


  lastEnergy: number = 0;
  startLayoutTimer() {
    if (!this._layoutTimer && this._paused == false) {
      this._layoutTimer = setInterval(() => {
        const energy = this._layouting.layout(5, 0.075);
        const energyDelta = Math.abs(energy - this.lastEnergy);
        this.lastEnergy = energy;
        this._bbox = this._layouting.getMinimalBbox();
        console.log(`energy: ${energy}, energyDelta: ${energyDelta}`);
        if (energy <= 5 || energyDelta <= 0.0005) {
          this.stopLayoutTimer();
          this.isGraphStable = true;
        }

      }, 25);
    }
  }

  stopLayoutTimer() {
    if (this._layoutTimer) {
      clearInterval(this._layoutTimer);
      this._layoutTimer = undefined;
    }
  }

}
