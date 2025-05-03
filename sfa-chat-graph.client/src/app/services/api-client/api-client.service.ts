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

  public async describeAsync(iri: string): Promise<SparqlStarResult> {
    return await firstValueFrom(this._httpClient.get<SparqlStarResult>(`/api/v1/chat/describe?subject=${encodeURIComponent(iri)}`));
  }

  public async getHistoryAsync(id: string, loadBlobs: boolean = false): Promise<ApiMessage[]> {
    return await firstValueFrom(this._httpClient.get<ApiMessage[]>(`/api/v1/chat/history/${id}?loadBlobs=${loadBlobs}`));
  }

  public async chatAsync(id: string, request: ChatRequest, eventChannel?: string): Promise<ApiMessage[]> {
    let endpoint = `/api/v1/chat/complete/${id}`;
    if (eventChannel)
      endpoint += `?eventChannel=${eventChannel}`;

    return await firstValueFrom(this._httpClient.post<ApiMessage[]>(endpoint, request));
  }


}
