import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';

@Component({
  selector: 'app-nav',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './nav.component.html',
  styleUrl: './nav.component.css'
})
export class NavComponent {
  auth = inject(AuthService);
  
  showNotifications = signal(false);
  toggleSidebar = signal<() => void>(() => {});

  notifications = [
    { id: 1, type: 'order', message: 'New order #12345 received', time: '5 minutes ago' },
    { id: 2, type: 'success', message: 'Payment confirmed for order #12340', time: '1 hour ago' },
    { id: 3, type: 'warning', message: 'Low stock alert: Product XYZ', time: '2 hours ago' },
    { id: 4, type: 'order', message: 'New order #12344 received', time: '3 hours ago' },
  ];

  toggleNotifications() {
    this.showNotifications.update(v => !v);
  }
}
