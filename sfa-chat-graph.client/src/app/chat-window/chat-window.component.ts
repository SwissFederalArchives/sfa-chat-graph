import { HttpClient } from '@angular/common/http';
import { AfterViewInit, Component, Input, OnInit, ViewChild } from '@angular/core';
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

  graph: Graph;
  chatId!: string;


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

  async ngAfterViewInit() {
    try {
      var history = await this.apiClient.getHistoryAsync(this.chatId);
      this.chatHistoryComponent.setupHistory(history);
    } catch (e) {
      console.log(e);
      this.chatHistoryComponent.error = "Error loading chat history";
    }
  }

  ngOnInit() {
    this.chatId = this.route.snapshot.paramMap.get("chatId")!;
  }



  title = 'sfa-chat-graph.client';
}
