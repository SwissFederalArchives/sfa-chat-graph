import { BBox } from "./bbox";
import { Graph } from "./graph";

export interface IGraphLayout {
  layout(steps: number, scale: number): number;
  getMinimalBbox(): BBox;
  notifyGraphUpdated(): void;
}
