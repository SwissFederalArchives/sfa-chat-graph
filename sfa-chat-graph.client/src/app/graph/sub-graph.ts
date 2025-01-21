import { Node } from './node';

export class SubGraph {
  public readonly nodes: Node[] = [];
  private _hidden: boolean = false;

  public isHidden(): boolean {
    return this._hidden;
  }

  public setHidden(hidden: boolean): void {
    this._hidden = hidden;
  }

  constructor(public readonly id: string, public readonly nodeColor: string, public readonly leafColor: string) {
  }

}
