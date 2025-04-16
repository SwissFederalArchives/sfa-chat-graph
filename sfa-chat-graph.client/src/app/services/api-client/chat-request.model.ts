import { ApiMessage } from "./chat-message.model";

export class ChatRequest {
  public maxErrors?: number = 3;
  public temperature?: number = undefined;
  public message: ApiMessage;

  constructor(message: ApiMessage) {
    this.message = message;
  }
}
