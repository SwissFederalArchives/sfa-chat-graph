import { HttpClient } from '@angular/common/http';
import { AfterViewInit, Component, HostListener, Input, OnInit, ViewChild } from '@angular/core';
import { ApiClientService } from '../services/api-client/api-client.service';
import { ActivatedRoute, Router } from '@angular/router';
import { ChatHistoryComponent } from '../chat-history/chat-history.component';
import { Graph } from '../graph/graph';
import { GraphVisualisationComponent } from '../graph-visualisation/graph-visualisation.component';
import { NgIf } from '@angular/common';
import { EventChannel } from '../services/api-client/event-channel.service';

@Component({
  selector: 'chat-window',
  standalone: true,
  imports: [GraphVisualisationComponent, ChatHistoryComponent, NgIf],
  templateUrl: './chat-window.component.html',
  styleUrl: './chat-window.component.css'
})
export class ChatWindowComponent implements OnInit, AfterViewInit {
  @ViewChild("history") chatHistoryComponent!: ChatHistoryComponent;

  public graph: Graph;
  public chatId!: string;
  public leftPaneWidth: number = 0;
  private _isResizing = false;

  constructor(private http: HttpClient, private apiClient: ApiClientService, private route: ActivatedRoute, private router: Router) {
    this.graph = new Graph();

    this.graph.onNodeDetailsRequested.subscribe(async (data) => {
      if (data.value) {
        if (data.value.node.isNoLeaf) {
          const graph = data.value.graph;
          let response = await this.apiClient.describeAsync(data.value.node.iri);
          graph.loadFromSparqlStar(response, 20, data.value.node.subGraph?.id, response.head.vars);
          data.next(graph);
        }
      }
    });
  }

  ngAfterViewInit(): void {
    const storedWidth = localStorage.getItem("chat-window:leftPaneWidth");
    if (storedWidth) {
      const stored = parseInt(storedWidth, 10);
      if(stored > window.innerWidth*0.1 && stored <= window.innerWidth * 0.9) {
        this.leftPaneWidth = stored;
        return;
      }
    }

    this.leftPaneWidth = window.innerWidth / 2;
  }

  startResizing(event: MouseEvent) {
    this._isResizing = true;
    event.preventDefault();
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent) {
    if (!this._isResizing) return;
    this.leftPaneWidth = event.clientX;
  }

  @HostListener('document:mouseup')
  stopResizing() {
    this._isResizing = false;
    localStorage.setItem("chat-window:leftPaneWidth", this.leftPaneWidth.toString());
  }

  ngOnInit() {
    this.chatId = this.route.snapshot.paramMap.get("chatId")!;
  }


  title = 'sfa-chat-graph.client';
}
