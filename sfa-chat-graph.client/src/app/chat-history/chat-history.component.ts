import { Component, ElementRef, Input, signal, Signal, ViewChild, WritableSignal } from '@angular/core';
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
import { MarkdownModule } from 'ngx-markdown';
import { CollapseContainerComponent } from "../collapse-container/collapse-container.component";


class SubGraphMarker {
  constructor(public readonly id: string, public readonly color: string, public label: string) { }
}

class DisplayMessage {
  id: string;
  message: string;
  cls: string;
  markers: SubGraphMarker[];
  queries: string[];

  constructor(id: string, message: string, cls: string, markers: SubGraphMarker[] | undefined = undefined, queries: string[] | undefined = undefined) {
    this.message = message;
    this.cls = cls;
    this.id = id;
    this.markers = markers ?? [];
    this.queries = queries ?? [];
  }
}

@Component({
  selector: 'chat-history',
  standalone: true,
  imports: [MatIcon, FormsModule, MarkdownModule, MatButton, MatIconButton, NgIf, NgFor, MatInputModule, CollapseContainerComponent],
  templateUrl: './chat-history.component.html',
  styleUrl: './chat-history.component.css'
})
export class ChatHistoryComponent {
  @Input() graph!: Graph;
  history: ApiMessage[] = [];
  displayHistory: DisplayMessage[] = [];

  waitingForResponse: boolean = false;
  message?: string = undefined;
  @ViewChild('chatHistory') chatHistory!: ElementRef<HTMLElement>;
  roles = ChatRole;

  addMessageToHistory(message: ApiMessage) {
    this.history.push(message);
    if (message.role == ChatRole.Assitant) {

      const previousResponseIndex = this.history.slice(0, -1).reverse().findIndex(m => m.role == ChatRole.Assitant);
      const subGraphs = this.history.slice(Math.max(0, previousResponseIndex), -1)
        .filter(m => m.role == ChatRole.ToolResponse && m.graphToolData && m.toolCallId)
        .map(msg => this.graph.getSubGraph(msg.toolCallId!))
        .filter(x => x)
        .map(subGraph => new SubGraphMarker(subGraph!.id, subGraph!.leafColor, subGraph!.id));

      const queries = this.history.slice(Math.max(0, previousResponseIndex), -1)
        .filter(m => m.role == ChatRole.ToolResponse && m.graphToolData)
        .map(msg => msg.graphToolData?.query)
        .filter(query => query)
        .map(query => `\`\`\`sparql\n${query}\n\`\`\``);

      this.displayHistory.push(new DisplayMessage(message.id, message.content!, 'chat-message-left', subGraphs, queries));
    } else if (message.role == ChatRole.User) {
      this.displayHistory.push(new DisplayMessage(message.id, message.content!, 'chat-message-right', undefined));
    }
  }

  scrollToBottom(){
    if (this.chatHistory) {
      this.chatHistory.nativeElement.scroll({
        top: this.chatHistory.nativeElement.scrollHeight,
        behavior: 'smooth'
      });
    }
  }

  constructor(private _apiClient: ApiClientService) {

  }

  async send() {
    this.waitingForResponse = true;
    try {
      this.addMessageToHistory(new ApiMessage(this.message));
      const request = new ChatRequest(this.history);
      const response = await this._apiClient.chatAsync(request);
      let sparqlLoaded: boolean = false;

      for (let sparql of response.filter(m => m.role == ChatRole.ToolResponse).filter(tc => tc && tc.graphToolData && tc.graphToolData.visualisationGraph)) {
        this.graph.loadFromSparqlStar(sparql!.graphToolData!.visualisationGraph!, 100, sparql!.toolCallId);
        sparqlLoaded = true;
      }

      if (sparqlLoaded)
        this.graph.updateModels();

      response.forEach(this.addMessageToHistory, this);
    } catch (e) {
      console.error(e);
    } finally {
      this.waitingForResponse = false;
      this.message = undefined;
    }
  }

}
