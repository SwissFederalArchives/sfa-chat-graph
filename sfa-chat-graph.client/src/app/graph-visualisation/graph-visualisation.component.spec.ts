import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GraphVisualisationComponent } from './graph-visualisation.component';

describe('GraphVisualisationComponent', () => {
  let component: GraphVisualisationComponent;
  let fixture: ComponentFixture<GraphVisualisationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GraphVisualisationComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GraphVisualisationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
