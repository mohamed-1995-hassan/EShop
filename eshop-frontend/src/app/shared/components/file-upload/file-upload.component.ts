import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface UploadedFile {
  file: File;
  preview?: string;
  progress?: number;
  error?: string;
}

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-upload.component.html',
  styleUrl: './file-upload.component.css'
})
export class FileUploadComponent {
  accept = input<string>('image/*');
  multiple = input<boolean>(false);
  maxSize = input<number | null>(null); // in bytes
  maxFiles = input<number>(10);
  
  filesChange = output<File[]>();
  fileAdded = output<File>();
  fileRemoved = output<File>();
  
  files = signal<UploadedFile[]>([]);
  isDragging = signal(false);
  
  acceptLabel(): string {
    const accept = this.accept();
    if (accept.includes('image/')) return 'PNG, JPG, GIF, WebP';
    if (accept.includes('video/')) return 'MP4, WebM, MOV';
    if (accept.includes('.pdf')) return 'PDF files';
    if (accept.includes('.doc')) return 'Word documents';
    return 'Supported files';
  }
  
  formatSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
  
  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(true);
  }
  
  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
  }
  
  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
    
    const files = event.dataTransfer?.files;
    if (files) {
      this.handleFiles(Array.from(files));
    }
  }
  
  onFileSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.handleFiles(Array.from(input.files));
      input.value = ''; // Reset input
    }
  }
  
  private handleFiles(newFiles: File[]) {
    const maxFiles = this.maxFiles();
    const maxSize = this.maxSize();
    const currentCount = this.files().length;
    
    for (const file of newFiles) {
      if (currentCount + this.files().length >= maxFiles) {
        console.warn(`Maximum ${maxFiles} files allowed`);
        break;
      }
      
      if (maxSize && file.size > maxSize) {
        console.warn(`File ${file.name} exceeds maximum size`);
        continue;
      }
      
      const uploadedFile: UploadedFile = { file };
      
      // Generate preview for images
      if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = (e) => {
          this.files.update(files => {
            const index = files.findIndex(f => f.file === file);
            if (index !== -1) {
              files[index].preview = e.target?.result as string;
            }
            return [...files];
          });
        };
        reader.readAsDataURL(file);
      }
      
      this.files.update(files => [...files, uploadedFile]);
      this.fileAdded.emit(file);
    }
    
    this.emitFiles();
  }
  
  removeFile(index: number) {
    const file = this.files()[index];
    this.files.update(files => files.filter((_, i) => i !== index));
    this.fileRemoved.emit(file.file);
    this.emitFiles();
  }
  
  private emitFiles() {
    this.filesChange.emit(this.files().map(f => f.file));
  }
  
  // Public method to update progress
  updateProgress(fileName: string, progress: number) {
    this.files.update(files => {
      const file = files.find(f => f.file.name === fileName);
      if (file) {
        file.progress = progress;
      }
      return [...files];
    });
  }
  
  // Public method to set error
  setError(fileName: string, error: string) {
    this.files.update(files => {
      const file = files.find(f => f.file.name === fileName);
      if (file) {
        file.error = error;
      }
      return [...files];
    });
  }
  
  // Public method to clear all files
  clear() {
    this.files.set([]);
    this.emitFiles();
  }
}
