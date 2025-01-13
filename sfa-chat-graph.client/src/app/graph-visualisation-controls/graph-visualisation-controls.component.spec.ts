import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GraphVisualisationControlsComponent } from './graph-visualisation-controls.component';

describe('GraphVisualisationControlsComponent', () => {
  let component: GraphVisualisationControlsComponent;
  let fixture: ComponentFixture<GraphVisualisationControlsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GraphVisualisationControlsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GraphVisualisationControlsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
