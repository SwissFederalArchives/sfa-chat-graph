import { BBox } from "./bbox";
import { Graph } from "./graph";
import { Node } from "./node";

export interface IGraphLayout {
  layout(steps: number, scale: number): number;
  getMinimalBbox(): BBox;
  notifyGraphUpdated(): void;
  relayoutLeafes(node: Node): void;
}
