import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GraphVisualisationPixijsComponentComponent } from './graph-visualisation-pixijs-component.component';

describe('GraphVisualisationPixijsComponentComponent', () => {
  let component: GraphVisualisationPixijsComponentComponent;
  let fixture: ComponentFixture<GraphVisualisationPixijsComponentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GraphVisualisationPixijsComponentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GraphVisualisationPixijsComponentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
