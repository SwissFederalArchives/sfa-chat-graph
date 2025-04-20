import { EventEmitter, Injectable, OnDestroy } from "@angular/core";
import { ApiChatEvent } from "./chat-message.model";

@Injectable({
    "providedIn": "root"
})
export class EventChannel implements OnDestroy{
    public readonly channelId: string;
    private socket?: WebSocket;
    public readonly onReceive: EventEmitter<ApiChatEvent> = new EventEmitter<ApiChatEvent>();

    public constructor() {
        this.channelId = window.crypto.randomUUID();
        this.socket = undefined;
        this.openAsync();
    }

    async ngOnDestroy(): Promise<void> {
        await this.closeAsync();
    }

    public isOpen(): boolean {
        return this.socket != undefined && this.socket.readyState === WebSocket.OPEN;
    }

    public async closeAsync(): Promise<void> {
        if (this.socket) {
            return await new Promise<void>((resolve, reject) => {
                this.socket!.onclose = () => { resolve() };
                this.socket!.close();
            });
        }
    }

    public async openAsync(): Promise<boolean> {
        if (!this.socket) {
            const socket = new WebSocket(`/api/v1/events/subscribe/${this.channelId}`);
            socket.onmessage = (event) => {
                const data = JSON.parse(event.data) as ApiChatEvent;
                this.onReceive.emit(data);
            };

            this.socket = socket;
            const prom = new Promise<boolean>((resolve, reject) => {
                socket.onopen = () => { resolve(true) };
                socket.onerror = (error) => {
                    console.error(error); 
                    resolve(false);
                }
            });
        }

        return true;
    }
}