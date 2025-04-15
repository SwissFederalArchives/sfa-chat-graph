import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChatDataPopoutComponent } from './chat-data-popout.component';

describe('ChatDataPopoutComponent', () => {
  let component: ChatDataPopoutComponent;
  let fixture: ComponentFixture<ChatDataPopoutComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ChatDataPopoutComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ChatDataPopoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
