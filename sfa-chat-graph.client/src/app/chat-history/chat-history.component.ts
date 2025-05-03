import { AfterViewChecked, AfterViewInit, Component, ElementRef, Inject, Injector, Input, OnChanges, OnDestroy, OnInit, signal, Signal, SimpleChanges, ViewChild, WritableSignal } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatButton } from '@angular/material/button';
import { MatIconButton } from '@angular/material/button';
import { NgFor, NgIf } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { ApiChatEvent, ApiMessage, ApiToolData, ChatRole } from '../services/api-client/chat-message.model';
import { Graph } from '../graph/graph';
import { ApiClientService } from '../services/api-client/api-client.service';
import { ChatRequest } from '../services/api-client/chat-request.model';
import { MarkdownModule } from 'ngx-markdown';
import { CollapseContainerComponent } from "../collapse-container/collapse-container.component";
import { ChatDataPopoutComponent } from '../chat-data-popout/chat-data-popout.component';
import { ActivatedRoute } from '@angular/router';
import { EventChannel } from '../services/api-client/event-channel.service';
import { DisplayMessage } from './DisplayMessage';
import { SubGraphMarker } from './SubGraphMarker';
import { DisplayDetail } from './DisplayDetail';


export enum ErrorType {
  ChatCompletion,
  FetchHistory,
}


@Component({
  selector: 'chat-history',
  standalone: true,
  imports: [MatIcon, FormsModule, MarkdownModule, NgIf, NgFor, MatInputModule],
  templateUrl: './chat-history.component.html',
  styleUrl: './chat-history.component.css'
})
export class ChatHistoryComponent implements AfterViewChecked, OnInit {

  @Input() graph!: Graph;
  @Input() chatId!: string;
  @ViewChild('chatHistory') chatContainer!: ElementRef<HTMLElement>;

  public messages: DisplayMessage[] = [];
  public waitingForResponse: boolean = false;
  public message?: string = undefined;
  public activity?: ApiChatEvent = undefined;
  public rolesEnum = ChatRole;

  public getError(): string | undefined { return this._error; }

  private _lastMesssage?: ApiMessage = undefined;
  private _toolData: Map<string, ApiToolData> = new Map<string, ApiToolData>();
  private _shouldScroll: boolean = false;
  private _error?: string = undefined;
  private _errorType?: ErrorType = undefined;

  constructor(private _apiClient: ApiClientService, private _injector: Injector, private _eventChannel: EventChannel) {
    this._eventChannel.onReceive.subscribe((event) => this.onChatEvent(event));
  }

  public async ngOnInit(): Promise<void> {
    await this.tryLoadHistory();
  }

  public ngAfterViewChecked(): void {
    if (this._shouldScroll) {
      this.scrollToBottom();
      this._shouldScroll = false;
    }
  }

  public onChatEvent(event: ApiChatEvent) {
    if (event.ChatId == this.chatId) {
      if (event.Done) {
        this.activity = undefined;
      } else {
        this.activity = event;
        this.setScrollToBottomFlag();
      }
    }
  }

  public async onMessageKeyPress($event: KeyboardEvent): Promise<void> {
    if($event.key == "Enter" && !$event.shiftKey) {
      $event.preventDefault();
      if (this.message && this.message.trim() != "") {
        return this.send();
      }
    }

    return Promise.resolve();
  }

  private async tryLoadHistory(): Promise<void> {
    if (this.waitingForResponse) return;
    this.waitingForResponse = true;
    this.activity = new ApiChatEvent("Fetching chat history");
    try {
      const history = await this._apiClient.getHistoryAsync(this.chatId);
      this.setChatHistory(history);
    } catch (e) {
      console.error(e);
      this._error = "Error loading chat history";
      this._errorType = ErrorType.FetchHistory;
    } finally {
      this.activity = undefined;
      this.waitingForResponse = false;
    }
  }

  private setChatHistory(messages: ApiMessage[]) {
    this.messages = [];
    this._toolData = new Map<string, ApiToolData>();
    this._error = undefined;
    messages.filter(m => m.role == ChatRole.ToolResponse && m.graphToolData && m.toolCallId)
      .forEach(m => this.graph.loadFromSparqlStar(m.graphToolData!.visualisationGraph!, 100, m.toolCallId));
    this.graph.updateModels();
    this.displayMessages(messages);
    this.setScrollToBottomFlag();
  }

  public setScrollToBottomFlag() {
    this._shouldScroll = this.isUserAtBottom();
  }

  public scrollToBottom(): void {
    if (this.chatContainer) {
      this.chatContainer.nativeElement.scroll({
        top: this.chatContainer.nativeElement.scrollHeight,
        behavior: 'smooth'
      });
    }
  }

  public showMessageDisplayDetail(data: DisplayDetail) {
    if (data.mimeType.startsWith("image/") || data.isBase64Content == false) {
      ChatDataPopoutComponent.showPopup(this._injector, data);
    } else {
      data.download();
    }
  }

  private isUserAtBottom(): boolean {
    const el = this.chatContainer.nativeElement;
    return el.scrollTop + el.clientHeight >= el.scrollHeight - 50;
  }

  private displayMessages(messages: ApiMessage[]) {
    const URL_SUBST_PATTERN: RegExp = new RegExp(/tool-data:\/\/([^\s()]+)/g);
    while (messages.length > 0) {
      const assistantIndex = messages.findIndex(m => m.role == ChatRole.Assitant || m.role == ChatRole.User);
      if (assistantIndex == -1) break;
      const message = messages[assistantIndex];
      if (message.role == ChatRole.Assitant) {

        const previousMessages = messages.slice(0, assistantIndex);
        const subGraphs = previousMessages
          .filter(m => m.role == ChatRole.ToolResponse && m.graphToolData && m.toolCallId)
          .map(msg => this.graph.getSubGraph(msg.toolCallId!))
          .filter(x => x)
          .map(subGraph => new SubGraphMarker(subGraph!.id, subGraph!.leafColor, subGraph!.id));

        const codeData = previousMessages
          .filter(m => m.role == ChatRole.ToolResponse && m.codeToolData)
          .map(m => m.codeToolData!)

        const graphData = previousMessages
          .filter(m => m.role == ChatRole.ToolResponse && m.graphToolData)
          .map(m => m.graphToolData!)

        codeData.flatMap(m => m.data)
          .filter(d => d && d.isBase64Content)
          .forEach(d => this._toolData.set(d!.id, d!))

        const content = message.content!.replaceAll(URL_SUBST_PATTERN, (match, id) => {
          const data = this._toolData.get(id);
          if (data && data.blobLoaded == false) {
            return `/api/v1/chat/tool-data/${data.id}`;
          } else if (data && data.blobLoaded && data.mimeType && data.isBase64Content) {
            return `data:${data.mimeType};base64,${data.content}`
          }
          return '';
        });

        this.messages.push(new DisplayMessage(message.id, content, 'chat-message-left', subGraphs, codeData, graphData));
      } else {
        this.messages.push(new DisplayMessage(message.id, message.content!, 'chat-message-right'));
      }

      messages = messages.slice(assistantIndex + 1);
    }
  }

  public async retry() {
    if (this.waitingForResponse) return;
    this._error = undefined;

    switch (this._errorType) {
      case ErrorType.ChatCompletion:
        await this.sendImpl();
        break;

      case ErrorType.FetchHistory:
        await this.tryLoadHistory();
        break;
    }
  }

  public async send() {
    if (this.waitingForResponse) return;
    await this.sendImpl();
  }

  private async sendImpl() {
    if (this.waitingForResponse) return;
    this.waitingForResponse = true;
    try {

      if (this._lastMesssage == undefined) {
        this._lastMesssage = new ApiMessage(this.message);
        this.displayMessages([this._lastMesssage]);
        this.message = undefined;
      }

      const request = new ChatRequest(this._lastMesssage);
      const response = await this._apiClient.chatAsync(this.chatId, request, this._eventChannel?.channelId);
      let sparqlLoaded: boolean = false;

      for (let sparql of response.filter(m => m.role == ChatRole.ToolResponse).filter(tc => tc && tc.graphToolData && tc.graphToolData.visualisationGraph)) {
        this.graph.loadFromSparqlStar(sparql!.graphToolData!.visualisationGraph!, 100, sparql!.toolCallId);
        sparqlLoaded = true;
      }

      if (sparqlLoaded)
        this.graph.updateModels();

      this.displayMessages(response);
      this._lastMesssage = undefined;
      this.message = undefined;
    } catch (e: any) {
      console.error(e);
      this._error = e.message ?? 'Unknown error occured';
      this._errorType = ErrorType.ChatCompletion;
    } finally {
      this.waitingForResponse = false;
      this.setScrollToBottomFlag();
    }
  }

}
