import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  type?: 'info' | 'warning' | 'danger';
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.css'
})
export class ConfirmDialogComponent {
  isOpen = signal(false);
  data = signal<ConfirmDialogData | null>(null);
  
  private resolvePromise: ((value: boolean) => void) | null = null;

  open(data: ConfirmDialogData): Promise<boolean> {
    this.data.set({ type: 'info', ...data });
    this.isOpen.set(true);
    
    return new Promise<boolean>(resolve => {
      this.resolvePromise = resolve;
    });
  }

  confirm() {
    this.isOpen.set(false);
    this.resolvePromise?.(true);
    this.resolvePromise = null;
  }

  cancel() {
    this.isOpen.set(false);
    this.resolvePromise?.(false);
    this.resolvePromise = null;
  }
}
