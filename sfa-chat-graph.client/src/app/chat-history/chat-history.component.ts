import { Component, ElementRef, Inject, Injector, Input, OnChanges, OnDestroy, signal, Signal, SimpleChanges, ViewChild, WritableSignal } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatButton } from '@angular/material/button';
import { MatIconButton } from '@angular/material/button';
import { NgFor, NgIf } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { ApiChatEvent, ApiCodeToolData, ApiGraphToolData, ApiMessage, ApiToolData, ChatRole } from '../services/api-client/chat-message.model';
import { Graph } from '../graph/graph';
import { ApiClientService } from '../services/api-client/api-client.service';
import { ChatRequest } from '../services/api-client/chat-request.model';
import { MarkdownModule } from 'ngx-markdown';
import { CollapseContainerComponent } from "../collapse-container/collapse-container.component";
import { downloadBlob, toBlob } from "../utils/utils"
import { Mime } from 'mime';
import standardTypes from 'mime/types/standard.js';
import otherTypes from 'mime/types/other.js';
import { ChatDataPopoutComponent } from '../chat-data-popout/chat-data-popout.component';
import { ActivatedRoute } from '@angular/router';
import { EventChannel } from '../services/api-client/event-channel.model';

const mime = new Mime(standardTypes, otherTypes);
mime.define({
  'application/python': ['py', 'python'],
  'application/x-sparqlstar-results+json': ['srj'],
  'application/sparql-query': ['rq', 'sparql'],
});

class SubGraphMarker {
  constructor(public readonly id: string, public readonly color: string, public label: string) { }
}

export class DisplayData {
  label: string;
  description?: string;
  content: string;
  mimeType: string;
  fileName: string;
  isBase64Content: boolean;
  className?: string;
  formattingLanguage?: string;

  constructor(label: string, contentString: string, isBase64Content: boolean, mimeType: string, description?: string, className?: string, formattingLanguage?: string) {
    this.label = label;
    this.mimeType = mimeType;
    this.fileName = `${encodeURIComponent(description?.replaceAll(" ", "_")?.toLowerCase() ?? window.crypto.randomUUID())}.${mime.getExtension(mimeType)}`;
    this.description = description;
    this.content = contentString;
    this.isBase64Content = isBase64Content;
    this.className = className;
    this.formattingLanguage = formattingLanguage;
  }

  public download(): void {
    const blob = toBlob(this.content, this.mimeType, this.isBase64Content);
    downloadBlob(blob, this.fileName);
  }

}

class DisplayMessage {
  id: string;
  message: string;
  cls: string;
  markers: SubGraphMarker[];
  data: DisplayData[] = [];


  private static *codeToDisplay(codes: ApiCodeToolData[]): Generator<DisplayData, void, unknown> {
    for (let i = 0; i < codes.length; i++) {
      const code = codes[i];
      const label = `Code ${i + 1}`;
      const res = code.success ? code.code : code.error;
      if (res) {
        const className = code.success ? 'tool-data-code' : 'tool-data-code-error';
        const display = new DisplayData(label, res, false, mime.getType(code.language!) || 'text/plain', 'Generated code for the visualisation', className, code.language);
        yield display;
      }

      for (let j = 0; j < (code.data?.length ?? 0); j++) {
        const data = code.data![j];
        if (data.content) {
          const type = mime.getExtension(data.mimeType!);
          const label = `Code ${i + 1} Data (${type}) ${j + 1}`;
          const display = new DisplayData(label, data.content, data.isBase64Content, data.mimeType!, data.description, 'tool-data-code-data');
          yield display;
        } else if (data.description) {
          const label = `Code ${i + 1} Output ${j + 1}`;
          const display = new DisplayData(label, data.description, false, 'text/plain', data.description, 'tool-data-code-ouput', undefined);
          yield display;
        }
      }
    }
  }

  private static *graphToDisplay(graphs: ApiGraphToolData[]): Generator<DisplayData, void, unknown> {
    for (let i = 0; i < graphs.length; i++) {
      const graph = graphs[i];
      if (graph.query) {
        const label = `Query ${i + 1}`;
        yield new DisplayData(label, graph.query, false, 'application/sparql-query', 'Generated SPARQL query for the visualisation', 'tool-data-graph-query', 'sparql');
      }

      if (graph.dataGraph) {
        const label = `Graph ${i + 1}`;
        const graphJson = JSON.stringify(graph.dataGraph, null, 2);
        yield new DisplayData(label, graphJson, false, 'application/x-sparqlstar-results+json', 'Generated data graph for the visualisation', 'tool-data-graph', 'json');
      }
    }
  }

  constructor(id: string, message: string, cls: string, markers?: SubGraphMarker[], code?: ApiCodeToolData[], graphs?: ApiGraphToolData[]) {
    this.message = message;
    this.cls = cls;
    this.id = id;
    this.markers = markers ?? [];

    if (code)
      this.data.push(...Array.from(DisplayMessage.codeToDisplay(code)));

    if (graphs)
      this.data.push(...Array.from(DisplayMessage.graphToDisplay(graphs)));
  }
}

@Component({
  selector: 'chat-history',
  standalone: true,
  imports: [MatIcon, FormsModule, MarkdownModule, NgIf, NgFor, MatInputModule],
  templateUrl: './chat-history.component.html',
  styleUrl: './chat-history.component.css'
})
export class ChatHistoryComponent {
  @Input() graph!: Graph;
  @Input() chatId!: string;

  history: ApiMessage[] = [];
  displayHistory: DisplayMessage[] = [];
  error?: string = undefined;
  toolData: Map<string, ApiToolData> = new Map<string, ApiToolData>();
  lastMesssage?: ApiMessage = undefined;
  activity?: string;

  waitingForResponse: boolean = false;
  message?: string = undefined;
  @ViewChild('chatHistory') chatHistory!: ElementRef<HTMLElement>;
  roles = ChatRole;

  constructor(private _apiClient: ApiClientService, private injector: Injector, private router: ActivatedRoute, private eventChannel: EventChannel) {
    this.eventChannel.onReceive.subscribe((event) => this.onChatEvent(event));
  }

  public onChatEvent(event: ApiChatEvent) {
    if (event.chatId == this.chatId) {
      if (event.done) {
        this.activity = undefined;
      }else{ 
        this.activity = event.activity;
      }
    }
  }

  public setupHistory(messages: ApiMessage[]) {
    this.history = [];
    this.displayHistory = [];
    this.toolData = new Map<string, ApiToolData>();
    this.error = undefined;
    this.displayMessages(messages);
    this.scrollToBottom();
  }

  displayMessages(messages: ApiMessage[]) {
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
          .forEach(d => this.toolData.set(d!.id, d!))

        const content = message.content!.replaceAll(URL_SUBST_PATTERN, (match, id) => {
          const data = this.toolData.get(id);
          if (data && data.isBase64Content && data.content && data.mimeType) {
            return `data:${data.mimeType};base64,${data.content}`;
          }
          return '';
        });

        this.displayHistory.push(new DisplayMessage(message.id, content, 'chat-message-left', subGraphs, codeData, graphData));
      } else {
        this.displayHistory.push(new DisplayMessage(message.id, message.content!, 'chat-message-right'));
      }
      messages = messages.slice(assistantIndex + 1);
    }
  }



  displayMessageData(data: DisplayData) {
    if (data.mimeType.startsWith("image/") || data.isBase64Content == false) {
      ChatDataPopoutComponent.showPopup(this.injector, data);
    } else {
      data.download();
    }
  }

  scrollToBottom() {
    if (this.chatHistory) {
      this.chatHistory.nativeElement.scroll({
        top: this.chatHistory.nativeElement.scrollHeight,
        behavior: 'smooth'
      });
    }
  }

  async resend() {
    if (this.waitingForResponse) return;
    this.error = undefined;
    await this.sendImpl();
  }

  async send() {
    if (this.waitingForResponse) return;
    await this.sendImpl();
  }

  async sendImpl() {
    if (this.waitingForResponse) return;
    this.waitingForResponse = true;
    try {

      if (this.lastMesssage == undefined) {
        this.lastMesssage = new ApiMessage(this.message);
        this.displayMessages([this.lastMesssage]);
      }

      const request = new ChatRequest(this.lastMesssage);
      const response = await this._apiClient.chatAsync(this.chatId, request, this.eventChannel?.channelId);
      let sparqlLoaded: boolean = false;

      for (let sparql of response.filter(m => m.role == ChatRole.ToolResponse).filter(tc => tc && tc.graphToolData && tc.graphToolData.visualisationGraph)) {
        this.graph.loadFromSparqlStar(sparql!.graphToolData!.visualisationGraph!, 100, sparql!.toolCallId);
        sparqlLoaded = true;
      }

      if (sparqlLoaded)
        this.graph.updateModels();

      this.displayMessages(response);
      this.lastMesssage = undefined;
      this.message = undefined;
    } catch (e: any) {
      console.error(e);
      this.error = e.message ?? 'Unknown error occured';
      this.scrollToBottom();
    } finally {
      this.waitingForResponse = false;
    }
  }

}
