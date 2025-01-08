import { Graph } from "./graph";

export class BBox {
  public readonly x: number;
  public readonly y: number;
  public readonly width: number;
  public readonly height: number;

  constructor(x: number, y: number, width: number, height: number) {
    this.x = x;
    this.y = y;
    this.width = width;
    this.height = height;
  }
}

export interface IGraphLayout {
  layout(steps: number, scale: number): number;
  getMinimalBbox(): BBox;
  notifyGraphUpdated(): void;
}
