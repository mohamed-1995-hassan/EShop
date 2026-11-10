import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ToastMessage {
  id: number;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message?: string;
  duration?: number;
}

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.css'
})
export class ToastComponent {
  private counter = 0;
  toasts = signal<ToastMessage[]>([]);

  show(type: ToastMessage['type'], title: string, message?: string, duration: number = 5000) {
    const id = ++this.counter;
    const toast: ToastMessage = { id, type, title, message, duration };
    
    this.toasts.update(t => [...t, toast]);
    
    if (duration > 0) {
      setTimeout(() => this.remove(id), duration);
    }
    
    return id;
  }

  success(title: string, message?: string, duration?: number) {
    return this.show('success', title, message, duration);
  }

  error(title: string, message?: string, duration?: number) {
    return this.show('error', title, message, duration);
  }

  warning(title: string, message?: string, duration?: number) {
    return this.show('warning', title, message, duration);
  }

  info(title: string, message?: string, duration?: number) {
    return this.show('info', title, message, duration);
  }

  remove(id: number) {
    this.toasts.update(t => t.filter(toast => toast.id !== id));
  }

  clear() {
    this.toasts.set([]);
  }
}
