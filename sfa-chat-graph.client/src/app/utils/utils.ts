export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

export function toBlob(data: string, mimeType: string, isBase64: boolean = true) {
  if (isBase64) {
    const latinString = atob(data);
    const array = Uint8Array.from(latinString, (c) => c.charCodeAt(0));
    return new Blob([array], { type: mimeType });
  } else {
    return new Blob([data], { type: `${mimeType};charset=utf-8` });
  }
}
