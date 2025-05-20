import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { SparqlStarResult } from './sparql-star-result.model';
import { catchError, firstValueFrom, map, of } from 'rxjs';
import { ChatRequest } from './chat-request.model';
import { ApiMessage } from './chat-message.model';
import { ApiResult } from './api-client.result-model';

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

  private blobToText(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = reject;
      reader.readAsText(blob);
    });
  }

  public async getToolDataAsync(id: string): Promise<string> {
     const data = await firstValueFrom(this._httpClient.get(`/api/v1/chat/tool-data/${id}`, { responseType: 'blob' })).then(this.blobToText);
     return data;
  }

  public async chatAsync(id: string, request: ChatRequest, eventChannel?: string): Promise<ApiResult<ApiMessage[]>> {
    let endpoint = `/api/v1/chat/complete/${id}`;
    if (eventChannel)
      endpoint += `?eventChannel=${eventChannel}`;

    return firstValueFrom(this._httpClient.post<ApiMessage[]>(endpoint, request)
      .pipe(
        map(x => ApiResult.FromResult<ApiMessage[]>(x)),
        catchError(x => of(ApiResult.FromError<ApiMessage[]>(x))),
      ));
  }


}
