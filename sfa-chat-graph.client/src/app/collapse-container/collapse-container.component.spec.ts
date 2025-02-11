import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CollapseContainerComponent } from './collapse-container.component';

describe('CollapseContainerComponent', () => {
  let component: CollapseContainerComponent;
  let fixture: ComponentFixture<CollapseContainerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CollapseContainerComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CollapseContainerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
