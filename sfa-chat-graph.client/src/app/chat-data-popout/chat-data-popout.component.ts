import { ApplicationRef, Component, ComponentRef, createComponent, EnvironmentInjector, HostListener, inject, Injector, Input, input, ViewContainerRef } from '@angular/core';
import { DisplayDetail } from '../chat-history/DisplayData';
import { MatIcon, MatIconModule } from '@angular/material/icon';
import { MatButton, MatIconButton } from '@angular/material/button';
import { NgIf } from '@angular/common';
import { MarkdownComponent } from 'ngx-markdown';
import Papa from 'papaparse';

@Component({
  selector: 'chat-data-popout',
  imports: [MatIcon, NgIf, MarkdownComponent],
  standalone: true,
  templateUrl: './chat-data-popout.component.html',
  styleUrl: './chat-data-popout.component.css'
})
export class ChatDataPopoutComponent {
  @Input() data?: DisplayDetail;
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

  public getIFrameContent(){
    if(this.data?.content){
      return `data:text/html;charset=utf-8,${encodeURIComponent(this.data?.content)}`;
    }

    return "";
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
    this.data = undefined;
    this.selfRef.destroy();
  }
}
