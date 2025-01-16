import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { GraphVisualisationComponent } from './graph-visualisation/graph-visualisation.component';
import { ChatHistoryComponent } from './chat-history/chat-history.component';
import { GraphVisualisationControlsComponent } from './graph-visualisation-controls/graph-visualisation-controls.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

@NgModule({
  declarations: [
    AppComponent,
  ],
  imports: [
    ChatHistoryComponent,
    BrowserModule,
    HttpClientModule,
    GraphVisualisationComponent,
    AppRoutingModule,
    ChatHistoryComponent
],
  providers: [
    provideAnimationsAsync()
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
