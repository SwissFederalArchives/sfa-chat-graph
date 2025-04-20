import { HttpClient } from '@angular/common/http';
import { Component, OnInit, ViewChild } from '@angular/core';
import { ApiClientService } from '../services/api-client/api-client.service';
import { ActivatedRoute, Router } from '@angular/router';
import { ChatHistoryComponent } from '../chat-history/chat-history.component';
import { Graph } from '../graph/graph';

@Component({
  selector: 'chat-window-component',
  standalone: true,
  templateUrl: './chat-window-component.component.html',
  styleUrl: './chat-window-component.component.css'
})
export class ChatWindowComponentComponent implements OnInit {
  @ViewChild("chat-history") chatHistoryComponent!: ChatHistoryComponent;

  graph?: Graph;
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

  async ngOnInit() {
    this.chatId = this.route.snapshot.paramMap.get("chatId")!;
    try {
      var history = await this.apiClient.getHistoryAsync(this.chatId);
      this.chatHistoryComponent.setChatHistory(history);
    } catch (e) {
      console.error(e);
      this.chatHistoryComponent._error = "Error loading chat history";
    }
  }



  title = 'sfa-chat-graph.client';
}
