import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GraphDetailComponentComponent } from './graph-detail-component.component';

describe('GraphDetailComponentComponent', () => {
  let component: GraphDetailComponentComponent;
  let fixture: ComponentFixture<GraphDetailComponentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GraphDetailComponentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GraphDetailComponentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
