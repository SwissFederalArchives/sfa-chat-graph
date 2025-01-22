import { SparqlStarResult } from "./sparql-star-result.model";

export class ApiMessage {
    public id!: string;
    public content?: string;
    public role!: ChatRole;
    public timestamp: Date = new Date(Date.now());
    public toolCalls?: ApiToolCall[];
    public toolCallId?: string;
    public graph?: SparqlStarResult;


    constructor(content?: string, role: ChatRole = ChatRole.User) {
        this.content = content;
        this.role = role;
    }
}

export class ApiToolCall {
    public toolCall?: string;
    public toolCallId?: string;
    public arguments?: object;
}

export enum ChatRole {
    User = 0,
    Assitant = 1,
    ToolCall = 2,
    ToolResponse = 3
}