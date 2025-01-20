import { ApiMessage } from "./chat-message.model";

export class ChatRequest {
    public maxErrors?: number = 3;
    public temperature?: number = undefined;
    public history: ApiMessage[] = [];

    constructor(history: ApiMessage[]|undefined = undefined) {
        this.history = history ?? [];
    }
}