import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { OrdersService, OrderDetailsDto } from '@core/services/orders.service';
import { catchError, of } from 'rxjs';

interface AdminOrder {
  id: string;
  orderNumber: string;
  customerName: string;
  status: string;
  total: number;
  itemCount: number;
  createdAt: string;
}

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './admin-orders.component.html',
  styleUrl: './admin-orders.component.css'
})
export class AdminOrdersComponent implements OnInit {
  private ordersService = inject(OrdersService);
  
  sidebarOpen = signal(true);
  orders = signal<AdminOrder[]>([]);
  loading = signal(true);
  statusFilter = '';
  
  ngOnInit() {
    this.loadOrders();
  }
  
  loadOrders() {
    this.loading.set(true);
    // Note: In a real app, you'd have an admin endpoint to get all orders
    // For now, we'll simulate with empty data that would come from a real API
    this.loading.set(false);
    // The actual implementation would call an admin orders endpoint
  }
  
  shipOrder(order: AdminOrder) {
    const trackingNumber = prompt('Enter tracking number:');
    if (trackingNumber) {
      this.ordersService.shipOrder(order.id, {
        trackingNumber,
        carrierName: 'Standard Shipping'
      }).subscribe({
        next: () => this.loadOrders(),
        error: (err) => console.error('Error shipping order:', err)
      });
    }
  }
  
  deliverOrder(orderId: string) {
    if (confirm('Mark this order as delivered?')) {
      this.ordersService.deliverOrder(orderId).subscribe({
        next: () => this.loadOrders(),
        error: (err) => console.error('Error delivering order:', err)
      });
    }
  }
}
