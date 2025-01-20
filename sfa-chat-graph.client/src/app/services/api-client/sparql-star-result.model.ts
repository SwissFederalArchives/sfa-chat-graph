import { Validators } from "@angular/forms";

export class SparqlStarResult {
    head: SparqlStarHead = new SparqlStarHead();
    results: SparqlStarResults = new SparqlStarResults();
}

export class SparqlStarHead {
    vars: string[] = [];
}

export class SparqlStarResults {
    bindings: SparqlStarBindings[] = [];
}

export class SparqlStarBindings {
    [key: string]: SparqlStarBinding;
}

export class SparqlStarBinding {
    type!: string;
    value!: string;
    datatype?: string;
    lang?: string;
}