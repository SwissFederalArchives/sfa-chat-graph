import { Component } from '@angular/core';
import { GraphVisualisationComponent } from '../graph-visualisation/graph-visualisation.component';
import { MatButtonModule } from '@angular/material/button';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { NgIf } from '@angular/common';

@Component({
  selector: 'graph-visualisation-controls',
  standalone: true,
  imports: [MatButtonModule, MatIcon, MatIconButton, NgIf],
  templateUrl: './graph-visualisation-controls.component.html',
  styleUrl: './graph-visualisation-controls.component.css'
})
export class GraphVisualisationControlsComponent {
  constructor(
    private _parent: GraphVisualisationComponent
  ) { }

  save() {
    const blob = new Blob([this._parent.svg.nativeElement.outerHTML], { type: 'image/svg+xml' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'graph.svg';
    a.click();
    document.body.removeChild(a);
  }

  togglePause() {
    this._parent.setPaused(!this._parent.isPaused());
  }

  isPaused() {
    return this._parent.isPaused();
  }

  collapseAll() {
    this._parent.graph.getCenterNodes().forEach(n => n.setCollapsed(true));
    if(this._parent.isPaused()){
      this.togglePause();
    }
    this._parent.startLayoutTimer();
  }

  expandAll() {
    this._parent.graph.getCenterNodes().forEach(n => n.setCollapsed(false));
    if(this._parent.isPaused()){
      this.togglePause();
    }
    this._parent.startLayoutTimer();
  }
}
