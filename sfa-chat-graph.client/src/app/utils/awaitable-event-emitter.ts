import { EventEmitter } from "@angular/core";

export class AwaitableEventEmitter<T, R> extends EventEmitter<{ value?: T, next: (value: R) => void }> {
    constructor(isAsync?: boolean) {
        super(isAsync);
    }

    emitAsync(value?: T): Promise<R> {
        let next: (value: R) => void = (value: R) => { return value; };
        const promise = new Promise<R>((resolve) => {
            next = resolve;
        });

        super.emit({ value, next });

        return promise;
    }

}