import { SparqlStarResult } from "./sparql-star-result.model";

export class ApiMessage {
  public id!: string;
  public content?: string;
  public role!: ChatRole;
  public timestamp: Date = new Date(Date.now());
  public toolCalls?: ApiToolCall[];
  public toolCallId?: string;
  public graphToolData?: ApiGraphToolData;
  public codeToolData?: ApiCodeToolData;

  constructor(content?: string, role: ChatRole = ChatRole.User) {
    this.content = content;
    this.role = role;
  }
}

export class ApiChatEvent {

  constructor(activity: string, detail?: string) {
    this.Activity = activity;
    this.Detail = detail;
  }

  public ChatId!: string;
  public TimeStamp!: Date;
  public Activity!: string;
  public Detail?: string;
  public Trace?: string;
  public Done!: boolean;
}

export class ApiGraphToolData {
  public query?: string;
  public dataGraph?: SparqlStarResult;
  public visualisationGraph?: SparqlStarResult;
}

export class ApiToolData {
  public id!: string;
  public description?: string;
  public mimeType?: string;
  public isBase64Content!: boolean;
  public content?: string;
  public blobLoaded!: boolean;
}

export class ApiCodeToolData {
  public success!: boolean;
  public code?: string;
  public error?: string;
  public language?: string;
  public data?: ApiToolData[];
}

export class ApiToolCall {
  public toolId?: string;
  public toolCallId?: string;
  public arguments?: any;
}

export enum ChatRole {
  User = 0,
  Assitant = 1,
  ToolCall = 2,
  ToolResponse = 3
}
