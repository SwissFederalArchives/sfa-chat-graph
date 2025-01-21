import { Component, Input, signal, Signal, ViewChild, WritableSignal } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatButton } from '@angular/material/button';
import { MatIconButton } from '@angular/material/button';
import { NgFor, NgIf } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { ApiMessage, ChatRole } from '../services/api-client/chat-message.model';
import { Graph } from '../graph/graph';
import { ApiClientService } from '../services/api-client/api-client.service';
import { ChatRequest } from '../services/api-client/chat-request.model';

@Component({
  selector: 'chat-history',
  standalone: true,
  imports: [MatIcon, FormsModule, MatButton, MatIconButton, NgIf, NgFor, MatInputModule],
  templateUrl: './chat-history.component.html',
  styleUrl: './chat-history.component.css'
})
export class ChatHistoryComponent {
  @Input() graph!: Graph;
  history: ApiMessage[] = [];
  waitingForResponse: boolean = false;
  message?: string = undefined;
  @ViewChild('chatHistory') chatHistory!: HTMLElement;

  getDisplayMessage(): { message: string, cls: string }[] {
    return this.history.filter(m => m.role == ChatRole.User || m.role == ChatRole.Assitant)
      .map(m => ({ message: m.content!, cls: m.role == ChatRole.User ? 'chat-message-right' : 'chat-message-left' }));
  }

  constructor(private _apiClient: ApiClientService) {

  }

  async send() {
    this.waitingForResponse = true;
    try {
      const msg = new ApiMessage(this.message);
      this.history.push(msg);
      const request = new ChatRequest(this.history);
      const response = await this._apiClient.chatAsync(request);
      let sparqlLoaded: boolean = false;
      
      for (let sparql of response.filter(m => m.role == ChatRole.ToolResponse).filter(tc => tc && tc.graph)) {
        this.graph.loadFromSparqlStar(sparql!.graph!, 100, sparql!.toolCallId);
        sparqlLoaded = true;
      }
      
      if (sparqlLoaded)
        this.graph.updateModels();
      
      this.history.push(...response);
      if(this.chatHistory){
        this.chatHistory.scroll({
          top: this.chatHistory.scrollHeight,
          behavior: 'smooth'
        });
      }
    } catch (e) {
      console.error(e);
    } finally {
      this.waitingForResponse = false;
      this.message = undefined;
    }
  }

}
