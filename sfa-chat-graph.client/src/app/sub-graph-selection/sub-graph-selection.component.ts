import { Component, Input } from '@angular/core';
import { Graph } from '../graph/graph';
import { NgFor } from '@angular/common';
import { SubGraph } from '../graph/sub-graph';
import { GraphVisualisationComponent } from '../graph-visualisation/graph-visualisation.component';

@Component({
  selector: 'sub-graph-selection',
  imports: [NgFor],
  standalone: true,
  templateUrl: './sub-graph-selection.component.html',
  styleUrl: './sub-graph-selection.component.css'
})
export class SubGraphSelectionComponent {


  constructor(
    private _parent: GraphVisualisationComponent
  ) { }

  getSubGraphs(): Iterable<SubGraph> {
    return this._parent.graph.getSubGraphs();
  }

  toggleSubgraph(subGraph: SubGraph) {
    subGraph.setHidden(!subGraph.isHidden());
    this._parent.startLayoutTimer();
  }

  handleChecked(event: Event, subGraph: SubGraph) {
    subGraph.setHidden(!(event.target as HTMLInputElement).checked);
    this._parent.startLayoutTimer();
  }

}
