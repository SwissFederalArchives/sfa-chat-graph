import { Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import * as PIXI from 'pixi.js';
import { Graph } from '../graph/graph';
import { IGraphLayout } from '../graph/graph-layout';
import { BBox } from '../graph/bbox';


@Component({
  selector: 'app-graph-visualisation-pixijs-component',
  imports: [],
  templateUrl: './graph-visualisation-pixijs-component.component.html',
  styleUrl: './graph-visualisation-pixijs-component.component.css'
})
export class GraphVisualisationPixijsComponentComponent implements OnInit {
  @Input() graph!: Graph;
  @Input() showDebug: boolean = false;
  @ViewChild("canvas") canvas!: ElementRef<HTMLElement>;

  private readonly MOVE_THRESHOLD: number = 5;

  private _layouting!: IGraphLayout;
  private _bbox!: BBox;


  ngOnInit(): void {
    const app = new PIXI.Application({
      resizeTo: this.canvas.nativeElement,
      background: '#ffffff',
      antialias: true,
      autoStart: true,
    });

    app.ticker.add(this.update);

    this.canvas.nativeElement.appendChild(app.view);
  }

  private update(args: any): any {
    const dt = args.deltaTime / 1000;
    this._layouting.layout(1, dt * 0.01);
  }

}
