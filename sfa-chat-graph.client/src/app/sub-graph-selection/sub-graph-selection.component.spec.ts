import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SubGraphSelectionComponent } from './sub-graph-selection.component';

describe('SubGraphSelectionComponent', () => {
  let component: SubGraphSelectionComponent;
  let fixture: ComponentFixture<SubGraphSelectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubGraphSelectionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SubGraphSelectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
