import { Component, ElementRef, Inject, Injector, Input, signal, Signal, ViewChild, WritableSignal } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatButton } from '@angular/material/button';
import { MatIconButton } from '@angular/material/button';
import { NgFor, NgIf } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { ApiCodeToolData, ApiGraphToolData, ApiMessage, ApiToolData, ChatRole } from '../services/api-client/chat-message.model';
import { Graph } from '../graph/graph';
import { ApiClientService } from '../services/api-client/api-client.service';
import { ChatRequest } from '../services/api-client/chat-request.model';
import { MarkdownModule } from 'ngx-markdown';
import { CollapseContainerComponent } from "../collapse-container/collapse-container.component";
import { downloadBlob, toBlob } from "../utils/utils"
import { Mime } from 'mime';
import standardTypes from 'mime/types/standard.js';
import otherTypes from 'mime/types/other.js';
import { ChatDataPopoutComponent } from '../chat-data-popout/chat-data-popout.component';

const mime = new Mime(standardTypes, otherTypes);
mime.define({
  'application/python': ['py', 'python'],
  'application/x-sparqlstar-results+json': ['srj'],
  'application/sparql-query': ['rq', 'sparql'],
});

class SubGraphMarker {
  constructor(public readonly id: string, public readonly color: string, public label: string) { }
}

export class DisplayData {
  label: string;
  description?: string;
  content: string;
  mimeType: string;
  fileName: string;
  isBase64Content: boolean;
  color?: string;
  formattingLanguage?: string;

  constructor(label: string, contentString: string, isBase64Content: boolean, mimeType: string, description?: string, color?: string, formattingLanguage?: string) {
    this.label = label;
    this.mimeType = mimeType;
    this.fileName = `${encodeURIComponent(description?.replaceAll(" ", "_")?.toLowerCase() ?? window.crypto.randomUUID())}.${mime.getExtension(mimeType)}`;
    this.description = description;
    this.content = contentString;
    this.isBase64Content = isBase64Content;
    this.color = color;
    this.formattingLanguage = formattingLanguage;
  }

  public download(): void {
    const blob = toBlob(this.content, this.mimeType, this.isBase64Content);
    downloadBlob(blob, this.fileName);
  }

}

class DisplayMessage {
  id: string;
  message: string;
  cls: string;
  markers: SubGraphMarker[];
  data: DisplayData[] = [];


  private static *codeToDisplay(codes: ApiCodeToolData[]): Generator<DisplayData, void, unknown> {
    for (let i = 0; i < codes.length; i++) {
      const code = codes[i];
      const label = `Code ${i + 1}`;
      const res = code.success ? code.code : code.error;
      if (res) {
        const color = code.success ? undefined : 'red';
        const display = new DisplayData(label, res, false, mime.getType(code.language!) || 'text/plain', 'Generated code for the visualisation', color, code.language);
        yield display;
      }

      for (let j = 0; j < (code.data?.length ?? 0); j++) {
        const data = code.data![j];
        if (data.content) {
          const label = `Data ${i + 1}.${j + 1}`;
          const display = new DisplayData(label, data.content, data.isBase64Content, data.mimeType!, data.description);
          yield display;
        } else if (data.description) {
          const label = `Output ${i + 1}.${j + 1}`;
          const display = new DisplayData(label, data.description, false, 'text/plain', data.description, undefined, undefined);
          yield display;
        }
      }
    }
  }

  private static *graphToDisplay(graphs: ApiGraphToolData[]): Generator<DisplayData, void, unknown> {
    for (let i = 0; i < graphs.length; i++) {
      const graph = graphs[i];
      if (graph.query) {
        const label = `Query ${i + 1}`;
        yield new DisplayData(label, graph.query, false, 'application/sparql-query', 'Generated SPARQL query for the visualisation', undefined, 'sparql');
      }

      if (graph.dataGraph) {
        const label = `Graph ${i + 1}`;
        const graphJson = JSON.stringify(graph.dataGraph, null, 2);
        yield new DisplayData(label, graphJson, false, 'application/x-sparqlstar-results+json', 'Generated data graph for the visualisation', undefined, 'json');
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

@Component({
  selector: 'chat-history',
  standalone: true,
  imports: [MatIcon, FormsModule, MarkdownModule, MatButton, MatIconButton, NgIf, NgFor, MatInputModule, CollapseContainerComponent],
  templateUrl: './chat-history.component.html',
  styleUrl: './chat-history.component.css'
})
export class ChatHistoryComponent {
  @Input() graph!: Graph;
  history: ApiMessage[] = [];
  displayHistory: DisplayMessage[] = [];
  error?: string = undefined;
  toolData: Map<string, ApiToolData> = new Map<string, ApiToolData>();

  waitingForResponse: boolean = false;
  message?: string = undefined;
  @ViewChild('chatHistory') chatHistory!: ElementRef<HTMLElement>;
  roles = ChatRole;

  constructor(private _apiClient: ApiClientService, private injector: Injector) {
    const toolMsg = new ApiMessage(undefined, ChatRole.ToolResponse);
    const code = `
    import pandas as pd
    import matplotlib.pyplot as plt
    import seaborn as sns
    import numpy as np

    df = pd.read_csv('data.csv')
    sns.set(style="whitegrid")
    plt.figure(figsize=(10, 6))
    sns.barplot(x='Category', y='Count', data=df, palette='viridis')
    plt.title('Fruit Count')
    plt.xlabel('Fruit')
    plt.ylabel('Count')
    plt.show()
    `;

    toolMsg.toolCallId = window.crypto.randomUUID();
    toolMsg.codeToolData = {
      success: true,
      code: code,
      error: undefined,
      language: 'python',
      data: [{
        description: 'Hello World',
        content: undefined,
        mimeType: undefined,
      }, {
        description: '<Figure with 2 axes>',
        content: 'iVBORw0KGgoAAAANSUhEUgAAA00AAAImCAYAAACPR2EBAAAAOnRFWHRTb2Z0d2FyZQBNYXRwbG90bGliIHZlcnNpb24zLjEwLjEsIGh0dHBzOi8vbWF0cGxvdGxpYi5vcmcvc2/+5QAAAAlwSFlzAAAPYQAAD2EBqD+naQAAMERJREFUeJzt3QuYVVXdP/A13AdvqYigZhK+ShiIFwheRc2KLLPCy1smmJqCqWmakbduGkSKVor6SmpmaGqKmtndyltewLuJJoTkBTQUNQW5nv/z2//nzDsDuByZYc6Zmc/nec4z5+y9Z5818+x19vmutfbaNaVSqZQAAABYow5rXgwAAEAQmgAAADKEJgAAgAyhCQAAIENoAgAAyBCaAAAAMoQmAACADKEJAAAgQ2gCoE1pC/dsbwt/A0Bb0qnSBQCgbTrllFPSjTfe+Lbrf/zjH6d99tmnSe+x9957pyFDhqSJEycWry+66KLUpUuXdOSRR77j7z722GPpyiuvTNOnT0+vvPJK6tmzZxo2bFgaM2ZMeu9735sq5d38DQC0DKEJgHVms802S5MnT17jum222abJ+499r7/++g2C2HHHHfeOv3fVVVelCRMmpA996EPpa1/7WhGY5s6dmy677LL0hz/8If3sZz9L/fr1S5XQ2L8BgJYjNAGwzkSPyaBBg9bZ/vv37/+uf+eBBx5I48ePT4ccckg6/fTT65ZHgProRz+aPvvZz6bTTjstTZs2rZlLC0Br5ZomACpq9OjR6eSTT07HH398EbAOP/zwdN9996Xtt9+++LnqtvGoPzwvhgGG2L7c+1R+vibRm7TBBhukk046abV1m2yySbG/j3zkI2nRokXFshUrVhQ9U/vtt18aOHBg2muvvdKkSZPSkiVL3rZcYdW/IUJYhLxHHnkkfe5zn0sDBgxIH/7wh4vylDX2bwCgZQlNAKxTy5cvX+2x6kQHv/3tb9N6662XLr744rW+lufaa68tfh544IF1z1cV73vXXXcV1y7V1taucZtPfvKT6dhjj03du3cvXn/rW99K3//+94teqChf9FBNnTo1HXPMMe96woaVK1emr371q8V7TJkyJe28887p7LPPTnfeeWej/wYAWp7heQCsM88//3zaYYcdVlse1xHFhAtlnTt3Tt/97neL4Xxh1R6mxigPA+zVq9fbDglcuHBh0UO01VZb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AKAdqYopx99OhKdLL720uNBr5MiRafLkycVMe/EcAACgJVR89rxVPfXUUw1eDxw4sLiHEwAAQCVUdU8TAABApQlNAAAAGUITAABAhtAEAACQITQBAABkCE0AAAAZQhMAAECG0AQAAJAhNAEAAGQITQAAABlCEwAAQIbQBAAAkCE0AQAAZAhNAAAAGUITAABAhtAEAACQITQBAABkCE0AAAAZQhMAAECG0AQAAJAhNAEAAGQITQAAABlCEwAAQIbQBAAAkCE0AQAAZAhNAAAAGUITAABAhtAEAACQITQBAABkCE0AAAAZQhMAAECG0AQAAJAhNAEAAGQITQAAABlCEwAAQIbQBAAAkCE0AQAAZAhNAAAAGUITAABAhtAEAACQITQBAABkCE0AAAAZQhMAAECG0AQAAJAhNAEAAGQITQAAABlCEwAAQIbQBAAAkCE0AQAAZAhNAAAAGUITAABAhtAEAACQITQBAABkCE0AAAAZQhMAAECG0AQAAJAhNAEAAGQITQAAABlCEwAAQIbQBAAAkCE0AQAAZAhNAAAAGUITAABAhtAEAACQITQBAABkCE0AAAAZQhMAAECG0AQAAJAhNAEAAGQITQAAABlCEwAAQIbQBAAAkCE0AQAAZAhNAAAAGUITAABAhtAEAACQITQBAABkCE0AAAAZQhMAAECG0AQAAJAhNAEAAGQITQAAABlCEwAAQIbQBAAAkCE0AQAAVHNoevnll9PXv/71NHTo0LTTTjulMWPGpNmzZ9etnzlzZho1alQaNGhQ2nvvvdOVV15Z0fICAADtS8VD07HHHpvmzp2bpkyZkq6//vrUrVu3dNhhh6XFixenhQsXpsMPPzxtvfXW6YYbbii2nTRpUvEcAACgJXRKFfTaa6+lLbfcMo0dOzZtt912xbJjjjkmfeYzn0lPP/10uueee1Lnzp3TmWeemTp16pT69u1bF7AOOOCAShYdAABoJyra07TRRhulc889ty4wvfLKK+mKK65IvXr1Sttuu22aMWNGGjJkSBGYymIY3zPPPJMWLFhQwZIDAADtRUV7mur75je/ma677rrUpUuXdPHFF6fu3bun+fPn1wWqsp49exY/582bl3r06FGh0gIAAO1F1YSmL37xi+lzn/tcuuqqq4prl66++ur01ltvFSGqvq5duxY/lyxZstbvVSqV0qJFi5pcZiClmpqaVFtbW+li0I7ENa/xOQ5UD+cCWuu5IPYRx2+rCU0xHC+MHz8+PfLII2nq1KnFpBBLly5tsF05LEVP1NpatmxZMSsf0HRxkuzfv3+li0E7MmfOnOJkCVQP5wJa87lg1U6aqgtNcQ1TTPbw8Y9/vO66pQ4dOhQB6qWXXiqubYqf9ZVfb7755mv9vjG5RDmkAU3TmNYZaE59+vTR0wRVxrmA1noumDVrVqO2q2hoiskcTjrppHTppZem4cOH1/UCPfHEE8U9meKapWuuuSatWLEidezYsVh/7733Fv+kTTfdtEkVuyk9VQBUjiFAANQ207mgsYG/orPnxSQPe+yxR/re976Xpk+fnv7xj3+kU045Jb3++uvFvZpiWvE33ngjnX766UUKnDZtWjG7XkxRDgAA0C5ubnveeeelYcOGpRNPPDEddNBB6dVXXy0mg9hiiy2K3qTohYoxiyNHjkyTJ09O48aNK54DAAC0hIpPBLHBBhuk73znO8VjTQYOHJiuvfbaFi8XAABAVfQ0AQAAVDOhCQAAIENoAgAAyBCaAAAAMoQmAACADKEJAAAgQ2gCAADIEJoAAAAyhCYAAIAMoQkAACBDaAIAAMgQmgAAADKEJgAAgAyhCQAAIENoAgAAyBCaAAAAMoQmAACADKEJAAAgQ2gCAADIEJoAAAAyhCYAAIAMoQkAACBDaAIAAMgQmgAAADKEJgAAgAyhCQAAIENoAgAAyBCaAAAAMoQmAACADKEJAAAgQ2gCAADIEJoAAAAyhCYAAIAMoQkAACBDaAIAAMgQmgAAAFo6NM2fP39d7BYAAKB1hKYPfOAD6dFHH13juhkzZqRPfOITTS0XAABAVejU2A0vv/zytGjRouJ5qVRKv/zlL9Mdd9yx2nYPPfRQ6tKlS/OWEgAAoNpD05IlS9LkyZOL5zU1NUVoWlWHDh3SBhtskL785S83bykBAACqPTRFECqHoX79+qXrrrsuDRw4cF2WDQAAoPWEpvqefPLJ5i8JAABAWwlN4e67705/+ctf0uLFi9PKlSsbrIvhexMmTGiO8gEAALS+0BSTQpx99tmpa9euaZNNNilCUn2rvgYAAGhXoWnq1Klpv/32S+PHjzdTHgAA0Kat1X2aFixYkA488ECBCQAAaPPWKjT1798/Pf30081fGgAAgLYwPO+0005LX/3qV1P37t3TjjvumGpra1fbZosttmiO8gEAALS+0HTwwQcXM+ZFeHq7SR9mzpzZ1LIBAAC0ztB01llnmSEPAABoF9YqNO2///7NXxIAAIC2EpqmT5/+jtsMHjx4bXYNAADQ+kPT6NGji+F5pVKpbtmqw/Vc0wQAALTb0HTllVeutmzRokVpxowZ6eabb04XXHBBc5QNAACgdYamIUOGrHH5XnvtVUxDfvHFF6dLLrmkqWUDAABonTe3zdl1113T/fff39y7BQAAaBuh6c9//nNab731mnu3AAAArWd43qGHHrrasrjZ7fz589Pzzz+fjjrqqOYoGwAAQOsMTfVnzSvr0KFD2m677dLYsWPTAQcc0BxlAwAAaJ2h6ec//3nzlwQAAKCthKayO+64o5j04fXXX0+bbLJJ2mWXXdLw4cObr3QAAACtMTQtXbo0HXPMMemuu+5KHTt2TBtvvHFauHBhMc340KFDi59dunRp/tICAAC0htnz4ua1DzzwQDr77LPTo48+WoSnRx55JH3/+99PDz/8cHGfpvYiJsCAluBYAwBoRT1Nv/71r9Nxxx2XPv3pT//fjjp1Sp/97GfTyy+/nH7xi1+kE044IbUHMQHGOZOuT88+t6DSRaENe+9WPdLXTz6w0sUAAGiX1io0vfLKK6l///5rXBfLX3zxxdSeRGCaPXtepYsBAABUy/C8rbfeuhietybTp09PvXv3bmq5AAAAWm9P0+c///k0ceLE1K1bt7TvvvumHj16pAULFhTD9n7yk58UQ/cAAADabWg6+OCD0xNPPJEmTZqUzj333AY3vR05cmQaM2ZMc5YRAACg9U05Pn78+HTEEUcU92l67bXXUk1NTfroRz+a+vbt2/ylBAAAaA3XND311FPpgAMOSD/96U+L1xGQotfpC1/4Qvrxj3+cTjrppDRnzpx1VVYAAIDqDU3PPfdcOvTQQ4trl/r06dNgXefOndO4cePSq6++WgSo9jZ7HgAA0HY1OjRNmTIlvec970k33nhj2meffRqsq62tTYcddli6/vrrU9euXdMll1yyLsoKAABQvaHpnnvuSUceeWTaZJNN3nabzTbbrLjO6e67726u8gEAALSO0PTSSy+lbbbZ5h2322677dL8+fObWi4AAIDWFZqihymC0ztZuHBh2mijjZpaLgAAgNYVmgYPHpymTZv2jtvddNNNqX///k0tFwAAQFVodGgaPXp0uu+++9LEiRPTkiVL1njvprPPPjvdcccd6ZBDDmnucgIAAFT3zW0HDBiQTj311DRhwoR08803p2HDhqWtttoqrVixIr3wwgtFoIqheSeccEIaPnz4ui01AABAtYWmED1I/fr1S5dddlm67bbb6nqc1ltvvbT77rsXM+ftuOOO66qsAAAA1R2awi677FI8wiuvvJI6deqUNtxww3VRNgAAgNYXmurL3bMJAACgXU0EAQAA0B4JTQAAABlCEwAAQIbQBAAAkCE0AQAAVHNoevXVV9O3vvWttMcee6Sdd945HXzwwWnGjBl16++55560//77F/d/2meffdKtt95a0fICAADtS8VD00knnZQeeuihdN5556UbbrghfeADH0hf+tKX0j//+c80e/bsNHbs2DR8+PA0bdq0dNBBB6Vx48YVQQoAAKDq79PUVHPnzk133313uvrqq+tumPvNb34z3XnnnemWW25JL7/8ctp+++3TiSeeWKzr27dveuKJJ9Kll16ahg0bVsmiAwAA7URFe5o23njjNGXKlDRgwIC6ZTU1NcXj9ddfL4bprRqOhg4dmh544IFUKpUqUGIAAKC9qWho2nDDDdOee+6ZunTpUrfs97//fdEDFUPy5s+fn3r16tXgd3r27JkWL16cFi5cWIESAwAA7U1Fh+et6sEHH0ynnnpqGjFiRNprr73SW2+91SBQhfLrpUuXrvX7RC/VokWLmlze6BGrra1t8n6gsaLBoNp6WdUDWlo11gNo75wLaK3ngthHHL+tJjT96U9/SieffHIxg96kSZOKZV27dl0tHJVfN6ViLlu2LM2cObOJJf7/Zejfv3+T9wONNWfOnOJDopqoB7S0aqwH0N45F9CazwWrdtJUbWiaOnVqGj9+fDGl+A9+8IO6gvfu3Tu99NJLDbaN1927d08bbLDBWr9f586d07bbbtvkcjcmlUJz6tOnT9W1sKsHtLRqrAfQ3jkX0FrPBbNmzWrUdhUPTTFz3llnnZVGjx6dTj/99AaVbtddd033339/g+3vvffeojeqQ4e1vxwr3iOCF7Q2hj6AegBAarZzQWMDf6dKd6tNmDAhfexjHyvux7RgwYK6dd26dSuC1MiRI4vhevHz9ttvT7/73e+KKccBAABaQkVDU8yUF9cX/fGPfywe9UVImjhxYrrooovSOeeck372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fXpFywYAALR9nVKVix6l0Lt37wbLe/bsWbfu3Vi2bFkqlUrp0UcfbZby1dTUpP85cFBavnxAs+wP1qRTp47pscceK47dahT14Mj/HpiWr9ih0kWhDevUsfrrwditd07LtlpR6aLQhnXuUL31IOrAjmlEGrDe8koXhTasQ+rUrHUgskEcu60+NC1evLj42aVLlwbLu3btml577bV3vb/yP6Ux/5zG2mij9ZptX5DTnMdtc9t4/e6VLgLtRFXXg1rnA9p3PViv8waVLgLtRE0z1YHYT5sITd26dau7tqn8PCxZsiTV1ta+6/3ttNNOzVo+AACgbav6a5rKw/JeeumlBsvj9eabb16hUgEAAO1F1YemmC1v/fXXT/fdd1/dstdffz098cQTafDgwRUtGwAA0PZV/fC8uJZp1KhRadKkSWmTTTZJW265ZTrnnHNSr1690ogRIypdPAAAoI2r+tAU4h5Ny5cvT2eccUZ66623ih6myy67LHXu3LnSRQMAANq4mlI1zlkJAABQJar+miYAAIBKEpoAAAAyhCYAAIAMoQkAACBDaAIAAMgQmgAAADKEJgAAgAyhqZ3be++90/bbb1/3+OAHP5g+/vGPp0svvbTSRYOq88Ybb6Qdd9wx/fd//3datmzZOqmPF1xwQbPvF96NFStWpKuvvjodeOCBaaeddkq77rpr+vznP5+uv/765NaOtCfLly9PP/vZz9L+++9f1IWhQ4emI444It17771128R3p2nTprVIeUaPHp1OOeWUFnkvVtdpDctoZ+IDIB7hrbfeSo8++mg644wzUm1tbTrkkEMqXTyoGrfeemvadNNN07///e/0xz/+MX3yk5+sdJGgWUVjwLHHHlucB4477ri0++67FyHqzjvvTBMnTkx//vOfi2DfsWPHShcV1qklS5akww8/PM2bNy8df/zxRWiK70g33HBDsfzss89O++23X4uWSd2rLKGJ1L1797TZZpvVvX7ve9+b7rvvvuKDQWiC/xN1Yvjw4emFF15I11xzjdBEm3PJJZekGTNmFL1K73//++uW9+3bNw0ZMiT9z//8T7rsssvSmDFjKlpOWNd+/OMfp6eeeir9+te/Tr17965bfvrppxejDr73ve8VowNa0nve854WfT8aMjyPNerWrVvd89dee63oeYovizvssEMaNmxY8Xrx4sXF+ghY/fv3T7fffnv61Kc+VQzx22effdKf/vSnFt1HiJP5Rz/60eL348PswgsvNJyEZjF79uz0yCOPpN122y2NGDGiOGbnzJlTtz6Ot4suuih96UtfSgMHDkwf+9jH0i9/+cu69TF8Y4899kjXXXdd0XofrZbRov/iiy++7Xs++OCDRcNF7G+vvfZK3/3ud4uTdVn0BnzhC18o9jV48OD0la98pQh0sDZWrlyZfv7znxdDkeoHprL4jP7MZz5TbPPss88Ww5IiZEWd+MhHPlIcm//4xz/S2LFji+MxPodj+eWXX96gpfywww5LU6ZMKerDgAED0qhRo4r6VfbKK6+kE088sRgW+KEPfShNmjQpHXrooQ2Grv7lL38pylmuaz/60Y/S0qVL69bHuSTWx3DaOFfEkKY4h0Bje1yjkSyOofqBqeyrX/1q+slPflL3XSnOBXFcx/Ec31GiXtT3Tsdr1KXzzz8/ffjDHy7OD88880xxTvnBD35QNM5FPbj//vtXG57nHNHCSrRrH/7wh0vnn39+g2WPPPJIadiwYaWrr766eH300UeXRo4cWXr44YdLzz77bOnmm28u7bDDDqWf/vSnxfp77723tN1225X23Xff0t/+9rfSnDlzSl/5yldKO++8c+mNN95osX3cdtttpcGDB5fuuuuu0vPPP1+69dZbi/U33XRTi/5PaZsmTpxYGjRoUGnx4sWlhQsXFsfWhAkTGtSlWHbBBReUZs+eXRyX/fr1K47DcMMNNxTrP/nJT5amT59e1LM4nj/1qU+Vli1btlp9nDlzZmngwIGliy++uKgP8TsHHXRQ8Vi5cmVp+fLlpaFDh5bOO++80r/+9a/S448/Xtp///1LX/ziFyv0H6K1mzVrVvE5XD5m1+SWW24ptnnmmWeKnx//+MdLTz/9dOnRRx8tLVq0qLTbbruVxo0bV+wrjtuzzz672O6JJ54ofj+O76gHY8aMKY7x+L199tmnNHr06GL9ihUrSgceeGBRNx566KHiuD7kkENK22+/fV3duP3224u68Ytf/KI0d+7c0p133lkaMWJE6fjjjy/Wv/zyy6UPfvCDpalTp5aee+650owZM0p777136bTTTmuR/yOtX3yGx3H7m9/85h23je3i3HDjjTcWn8UXXnhhsSy+yzTmeC3v40Mf+lBRH+K4L58P4ji+++67i+VLliwpjRo1qvSNb3yjWO8c0fIMz6NoESm3BEbrSjyida48VjdaEaOFIlpCwlZbbZWmTp1atCiu2vISLXrhmGOOSb///e+LbaKFoyX28a9//St16dIlbbnllmmLLbYoHj179ix+QlMvBv7Vr35VtPxFy2I8ojXwpptuSieddFLq2rVrsV0si+tAQrTUR89UXERcHsYXdStaDqMFPpxzzjnFunvuuadonawvek3jmD/66KOL19tss00699xzi57UaHHs169fWrhwYXGMxzEfw2qj9fLll19u4f8ObUW5J2bjjTd+223K66I3KEQr9rbbblu3LHqEouV7vfXWK5bFtSAxsVAMc/rABz5QV5/iepCNNtqoeB2TTERdCHFsR+v4b3/727rerjiu6w+D+t///d9imGD8Xth6662LFvYvfvGL6bnnnkv/+c9/ilb8+OyPuhGP+J24NgveTV0oH6PvJOrBZz/72brvLvGd6vHHHy++z7zT8RrfZUL04kZPVX177rlnMfHQmjhHtDyhiaIiR5dv+WQ2d+7c9MMf/rA48cXwovgwiIt/b7zxxqLLeNasWUVFX3X4Rv3X66+/fvGzPMNYS+zj05/+dNGdHrP/xUk8PmjiudBEU8VQnwULFqR99923blk8jyEX8eWufLKMIRT1Rdj/61//Wvc6vkiWA1P5OpE4KUfwXzU0PfHEE0VdjH2sKoYyxXsdeeSR6ayzziqGdcSsTnGC/cQnPtGsfzvtRzkQReh4py+Tm2yySfHzfe97X926WBaf03ENSBy/0ZD15JNP1g39K+vRo0eDL6MbbLBB3ed8/F6sq38uiO379OlT9zq2iWAV112VlYdhR92IehDDvOPLZFyvG18sY+hSDIuCxigf36+++mqjto/AUt+GG25YTCTRmOO1HJrq16WyNS0rc45oeUITxQmqfsUsf5GLk9/f/va3dNVVV6Wnn366OAlFq3hcT/TNb35ztf1EL8+q4oMhTpYxxn1d7yM+5G6++eb00EMPpbvvvjvddddd6corryzG8JZb/2FtlKeTXdNxFBNClENTp04NP1LjuO3Q4f8uHe3cufNqvx+t32uaDSl+N3p7y62Iazqhn3zyyUU9jVAXvVVxcoxW/egBW1NdgpxoAY+QMX369OK6vTWJFuzYpqamZrXrX2NWyc997nPF8Rk9Q9HzGi3n8UWtvtyxGXWhfsBak1gfXwZH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',
        mimeType: 'image/png',
        isBase64Content: true,
        id: 'test-id'
      }, {
        description: 'csv data',
        content: 'a;b;c\n1;2;3\n4;5;6',
        mimeType: 'text/csv',
        isBase64Content: false,
        id: 'test-id-csv'
      }]
    } as ApiCodeToolData;
    this.addMessageToHistory(new ApiMessage("Pls plot me stuff", ChatRole.User));
    this.addMessageToHistory(toolMsg);
    this.addMessageToHistory(new ApiMessage("I've found the following code and produced a chart ![chart](tool-data://test-id)", ChatRole.Assitant));
    this.error = "Backend not reachable";
  }

  addMessageToHistory(message: ApiMessage) {
    const URL_SUBST_PATTERN: RegExp = new RegExp(/tool-data:\/\/([^\s()]+)/g);
    this.history.push(message);
    if (message.role == ChatRole.Assitant) {

      const previousResponseIndex = this.history.slice(0, -1).reverse().findIndex(m => m.role == ChatRole.Assitant);
      const previousMessages = this.history.slice(Math.max(0, previousResponseIndex), -1);
      const subGraphs = previousMessages
        .filter(m => m.role == ChatRole.ToolResponse && m.graphToolData && m.toolCallId)
        .map(msg => this.graph.getSubGraph(msg.toolCallId!))
        .filter(x => x)
        .map(subGraph => new SubGraphMarker(subGraph!.id, subGraph!.leafColor, subGraph!.id));

      const codeData = previousMessages
        .filter(m => m.role == ChatRole.ToolResponse && m.codeToolData)
        .map(m => m.codeToolData!)

      const graphData = previousMessages
        .filter(m => m.role == ChatRole.ToolResponse && m.graphToolData)
        .map(m => m.graphToolData!)

      codeData.flatMap(m => m.data)
        .filter(d => d && d.isBase64Content)
        .forEach(d => this.toolData.set(d!.id, d!))

      const content = message.content!.replaceAll(URL_SUBST_PATTERN, (match, id) => {
        const data = this.toolData.get(id);
        if (data && data.isBase64Content && data.content && data.mimeType) {
          return `data:${data.mimeType};base64,${data.content}`;
        }
        return '';
      });

      this.displayHistory.push(new DisplayMessage(message.id, content, 'chat-message-left', subGraphs, codeData, graphData));
    } else if (message.role == ChatRole.User) {
      this.displayHistory.push(new DisplayMessage(message.id, message.content!, 'chat-message-right'));
    }
  }



  displayMessageData(data: DisplayData) {
    if (data.mimeType.startsWith("image/") || data.isBase64Content == false) {
      ChatDataPopoutComponent.showPopup(this.injector, data);
    } else {
      data.download();
    }
  }

  scrollToBottom() {
    if (this.chatHistory) {
      this.chatHistory.nativeElement.scroll({
        top: this.chatHistory.nativeElement.scrollHeight,
        behavior: 'smooth'
      });
    }
  }

  async resend() {
    if (this.waitingForResponse) return;
    this.error = undefined;
    await this.sendImpl();
  }

  async send() {
    if(this.waitingForResponse) return;
    this.addMessageToHistory(new ApiMessage(this.message));
    await this.sendImpl();
  }

  async sendImpl(){
    if(this.waitingForResponse) return;
    this.waitingForResponse = true;
    try {
      const request = new ChatRequest(this.history);
      const response = await this._apiClient.chatAsync(request);
      let sparqlLoaded: boolean = false;

      for (let sparql of response.filter(m => m.role == ChatRole.ToolResponse).filter(tc => tc && tc.graphToolData && tc.graphToolData.visualisationGraph)) {
        this.graph.loadFromSparqlStar(sparql!.graphToolData!.visualisationGraph!, 100, sparql!.toolCallId);
        sparqlLoaded = true;
      }

      if (sparqlLoaded)
        this.graph.updateModels();

      response.forEach(this.addMessageToHistory, this);
    } catch (e: any) {
      console.error(e);
      this.error = e.message ?? 'Unknown error occured';
    } finally {
      this.waitingForResponse = false;
      this.message = undefined;
    }
  }

}
