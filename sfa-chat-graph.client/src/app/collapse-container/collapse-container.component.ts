import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'collapse-container',
  standalone: true,
  imports: [NgIf, MatIcon],
  templateUrl: './collapse-container.component.html',
  styleUrl: './collapse-container.component.css'
})
export class CollapseContainerComponent {
  @Input() collapsed: boolean = false;
  @Input() title: string = '';

  public toggle() {
    this.collapsed = !this.collapsed;
  }
}
