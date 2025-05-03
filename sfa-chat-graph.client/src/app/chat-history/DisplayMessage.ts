import { ApiCodeToolData, ApiGraphToolData } from '../services/api-client/chat-message.model';
import { DisplayDetail } from './DisplayDetail';
import { SubGraphMarker } from './SubGraphMarker';
import { Mime } from 'mime';
import standardTypes from 'mime/types/standard.js';
import otherTypes from 'mime/types/other.js';

export const mime = new Mime(standardTypes, otherTypes);
mime.define({
  'application/python': ['py', 'python'],
  'application/x-sparqlstar-results+json': ['srj'],
  'application/sparql-query': ['rq', 'sparql'],
});

export class DisplayMessage {
  id: string;
  message: string;
  cls: string;
  markers: SubGraphMarker[];
  data: DisplayDetail[] = [];


  private static *codeToDisplay(codes: ApiCodeToolData[]): Generator<DisplayDetail, void, unknown> {
    for (let i = 0; i < codes.length; i++) {
      const code = codes[i];
      const label = `Code ${i + 1}`;
      if (code.code) {
        const className = code.success ? 'tool-data-code' : 'tool-data-code-error';
        const display = new DisplayDetail(label, code.code, false, false, mime.getType(code.language!) || 'text/plain', 'Generated code for the visualisation', className, code.language, code.error);
        yield display;
      }

      for (let j = 0; j < (code.data?.length ?? 0); j++) {
        const data = code.data![j];
        let content = data.content;
        if(data.blobLoaded == false)
          content = '/api/v1/chat/tool-data/' + data.id;

        if (content) {
          const type = mime.getExtension(data.mimeType!);
          const label = `Code ${i + 1} Data (${type}) ${j + 1}`;
          const display = new DisplayDetail(label, content, data.isBase64Content, data.blobLoaded == false, data.mimeType!, data.description, 'tool-data-code-data');
          yield display;
        } else if (data.description) {
          const label = `Code ${i + 1} Output ${j + 1}`;
          const display = new DisplayDetail(label, data.description, false, false, 'text/plain', data.description, 'tool-data-code-ouput', undefined);
          yield display;
        }
      }
    }
  }

  private static *graphToDisplay(graphs: ApiGraphToolData[]): Generator<DisplayDetail, void, unknown> {
    for (let i = 0; i < graphs.length; i++) {
      const graph = graphs[i];
      if (graph.query) {
        const label = `Query ${i + 1}`;
        yield new DisplayDetail(label, graph.query, false, false, 'application/sparql-query', 'Generated SPARQL query for the visualisation', 'tool-data-graph-query', 'sparql');
      }

      if (graph.dataGraph) {
        const label = `Graph ${i + 1}`;
        const graphJson = JSON.stringify(graph.dataGraph, null, 2);
        yield new DisplayDetail(label, graphJson, false, false, 'application/x-sparqlstar-results+json', 'Generated data graph for the visualisation', 'tool-data-graph', 'json');
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
