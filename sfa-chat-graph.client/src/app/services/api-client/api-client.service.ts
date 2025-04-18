import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { SparqlStarResult } from './sparql-star-result.model';
import { firstValueFrom } from 'rxjs';
import { ChatRequest } from './chat-request.model';
import { ApiMessage } from './chat-message.model';

@Injectable({
  providedIn: 'root'
})
export class ApiClientService {

  constructor(private _httpClient: HttpClient) { }

  public openEventChannelAsync(channelId: string): Promise<WebSocket> {
    const socket = new WebSocket(`wss://localhost:40112/api/v1/events/subscribe/${channelId}`);
    const promise = new Promise<WebSocket>((resolve, reject) => {
      socket.onopen = () => resolve(socket);
      socket.onerror = (error) => reject(error);
    });

    return promise;
  }

  public async describeAsync(iri: string): Promise<SparqlStarResult> {
    return await firstValueFrom(this._httpClient.get<SparqlStarResult>(`https://localhost:40112/api/v1/rdf/describe?subject=${encodeURIComponent(iri)}`));
  }

  public async getHistoryAsync(id: string): Promise<ApiMessage[]> {
    return await firstValueFrom(this._httpClient.get<ApiMessage[]>(`https://localhost:40112/api/v1/rdf/history/${id}`));
  }

  public async chatAsync(id: string, request: ChatRequest, eventChannel?: string): Promise<ApiMessage[]>{
    let endpoint = `https://localhost:40112/api/v1/rdf/chat/${id}`;
    if (eventChannel)
      endpoint += `?eventChannel=${eventChannel}`;

    return await firstValueFrom(this._httpClient.post<ApiMessage[]>(endpoint, request));
  }


}
