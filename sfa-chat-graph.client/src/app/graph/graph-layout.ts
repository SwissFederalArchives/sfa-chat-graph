import { Graph } from "./graph";

export interface IGraphLayout {
  layout(graph: Graph): void;
}
