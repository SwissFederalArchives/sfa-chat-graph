import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AppComponent } from './app.component';
import { ChatWindowComponent } from './chat-window/chat-window.component';

const routes: Routes = [{
  path: 'chat/:chatId',
  component: ChatWindowComponent
}];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    initialNavigation: 'enabledBlocking',
    useHash: false
  })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
