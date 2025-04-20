import { toBlob, downloadBlob } from '../utils/utils';
import { mime } from './DisplayMessage';


export class DisplayDetail {
  label: string;
  description?: string;
  content: string;
  mimeType: string;
  fileName: string;
  isBase64Content: boolean;
  className?: string;
  formattingLanguage?: string;
  error?: string;


  constructor(label: string, contentString: string, isBase64Content: boolean, mimeType: string, description?: string, className?: string, formattingLanguage?: string, error?:string ) {
    this.label = label;
    this.mimeType = mimeType;
    this.fileName = `${encodeURIComponent(description?.replaceAll(" ", "_")?.toLowerCase() ?? window.crypto.randomUUID())}.${mime.getExtension(mimeType)}`;
    this.description = description;
    this.content = contentString;
    this.isBase64Content = isBase64Content;
    this.className = className;
    this.formattingLanguage = formattingLanguage;
    this.error = error;
  }

  public download(): void {
    const blob = toBlob(this.content, this.mimeType, this.isBase64Content);
    downloadBlob(blob, this.fileName);
  }

}
