import { EventEmitter } from "@angular/core";

export class AwaitableEventEmitter<T> extends EventEmitter {
    constructor(isAsync?: boolean) {
        super(isAsync);
    }

    override emit(value?: T) {
        let next: (value: unknown) => void = () => { };
        const promise = new Promise((resolve) => {
            next = resolve;
        });

        super.emit({ value, next });

        return promise;
    }

}