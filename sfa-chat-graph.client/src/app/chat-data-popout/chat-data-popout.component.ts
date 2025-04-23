import { ApplicationRef, Component, ComponentRef, createComponent, EnvironmentInjector, HostListener, inject, Injector, Input, input, ViewContainerRef } from '@angular/core';
import { DisplayDetail } from '../chat-history/DisplayDetail';
import { MatIcon, MatIconModule } from '@angular/material/icon';
import { MatButton, MatIconButton } from '@angular/material/button';
import { NgIf } from '@angular/common';
import { MarkdownComponent } from 'ngx-markdown';
import Papa from 'papaparse';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'chat-data-popout',
  imports: [MatIcon, NgIf, MarkdownComponent],
  standalone: true,
  templateUrl: './chat-data-popout.component.html',
  styleUrl: './chat-data-popout.component.css'
})
export class ChatDataPopoutComponent {
  @Input() data!: DisplayDetail;
  @Input() selfRef!: ComponentRef<ChatDataPopoutComponent>;

  static showPopup(injector: Injector, data: DisplayDetail): ChatDataPopoutComponent {
    const appRef = injector.get(ApplicationRef);
    const compRef = createComponent(ChatDataPopoutComponent, { environmentInjector: injector.get(EnvironmentInjector), elementInjector: injector });
    compRef.instance.data = data;
    compRef.instance.selfRef = compRef;
    appRef.attachView(compRef.hostView);
    const domElement = (compRef.hostView as any).rootNodes[0] as HTMLElement;
    document.body.appendChild(domElement);
    compRef.changeDetectorRef.detectChanges();
    return compRef.instance;
  }

  constructor(private _httpClient: HttpClient) { }

  @HostListener('window:keydown', ['$event'])
  handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      this.close();
    }
  }

  private csvToMdTable(csv: string): string {
    const data = Papa.parse(csv)
    const headerRow = data.data[0] as any;
    const header = `| ${headerRow.join(' | ')} |`;
    const separator = `| ${headerRow.map(() => '---').join(' | ')} |`;
    const rows = data.data.slice(1).map((row: any) => `| ${row.join(' | ')} |`).join('\n');
    return `${header}\n${separator}\n${rows}`;
  }

  private formatTextData(content?: string, mime?: string) {
    if (content) {
      if (mime == "text/csv")
        return this.csvToMdTable(content);

      return content;
    }

    return "";
  }

  public getImgContent(): string {
    if(this.data.isUrl)
      return this.data.content;

    if(this.data.isBase64Content)
      return `data:${this.data.mimeType};base64,${this.data.content}`;

    return this.data.content;
  }

  public async getHtmlContent() {
    if (this.data?.isUrl)
      return await firstValueFrom(this._httpClient.get(this.data?.content, { responseType: 'text' }))

    return this.data?.content;
  }

  public htmlEncode(content?: string): string {
    if (!content) return "";
    return content.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#039;');
  }


  public getMdContent() {
    if (this.data?.formattingLanguage) {
      return `\`\`\`${this.data?.formattingLanguage}\n${this.htmlEncode(this.data?.content)}\n\`\`\``;
    } else {
      return this.formatTextData(this.data?.content, this.data?.mimeType);
    }
  }

  public download(): void {
    this.data?.download();
  }

  public close(): void {
    this.selfRef.destroy();
  }
}
