import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

export type NotificationType = 'success' | 'error' | 'warning' | 'info';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private snackBar = inject(MatSnackBar);
  
  private defaultConfig: MatSnackBarConfig = {
    duration: 5000,
    horizontalPosition: 'end',
    verticalPosition: 'top'
  };
  
  success(message: string, action = 'Close'): void {
    this.show(message, action, 'success');
  }
  
  error(message: string, action = 'Close'): void {
    this.show(message, action, 'error');
  }
  
  warning(message: string, action = 'Close'): void {
    this.show(message, action, 'warning');
  }
  
  info(message: string, action = 'Close'): void {
    this.show(message, action, 'info');
  }
  
  private show(message: string, action: string, type: NotificationType): void {
    const config: MatSnackBarConfig = {
      ...this.defaultConfig,
      panelClass: [`snackbar-${type}`]
    };
    
    this.snackBar.open(message, action, config);
  }
}
