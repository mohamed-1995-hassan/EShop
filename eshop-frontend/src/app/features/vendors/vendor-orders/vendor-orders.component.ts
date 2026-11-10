import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';

interface VendorOrder {
  id: string;
  orderNumber: string;
  customerName: string;
  customerEmail: string;
  items: { name: string; quantity: number; price: number }[];
  subtotal: number;
  commission: number;
  netAmount: number;
  status: string;
  shippingAddress: string;
  createdAt: string;
}

@Component({
  selector: 'app-vendor-orders',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './vendor-orders.component.html',
  styleUrl: './vendor-orders.component.css'
})
export class VendorOrdersComponent implements OnInit {
  sidebarOpen = signal(true);
  loading = signal(true);
  
  orders = signal<VendorOrder[]>([]);
  selectedOrder = signal<VendorOrder | null>(null);
  showDetailsModal = signal(false);
  
  filterStatus = '';
  
  ngOnInit() {
    this.loadOrders();
  }
  
  loadOrders() {
    this.loading.set(true);
    setTimeout(() => {
      this.orders.set([
        { id: '1', orderNumber: 'ORD-2024-001', customerName: 'John Doe', customerEmail: 'john@example.com', items: [{ name: 'Wireless Headphones Pro', quantity: 1, price: 129.99 }], subtotal: 129.99, commission: 12.99, netAmount: 117.00, status: 'Pending', shippingAddress: '123 Main St, New York, NY 10001', createdAt: '2024-03-15T10:00:00Z' },
        { id: '2', orderNumber: 'ORD-2024-002', customerName: 'Jane Smith', customerEmail: 'jane@example.com', items: [{ name: 'Bluetooth Speaker Mini', quantity: 2, price: 49.99 }], subtotal: 99.98, commission: 9.99, netAmount: 89.99, status: 'Processing', shippingAddress: '456 Oak Ave, Los Angeles, CA 90001', createdAt: '2024-03-14T15:30:00Z' },
        { id: '3', orderNumber: 'ORD-2024-003', customerName: 'Bob Wilson', customerEmail: 'bob@example.com', items: [{ name: 'USB-C Hub 7-in-1', quantity: 1, price: 59.99 }, { name: 'Wireless Headphones Pro', quantity: 1, price: 129.99 }], subtotal: 189.98, commission: 18.99, netAmount: 170.99, status: 'Shipped', shippingAddress: '789 Pine Rd, Chicago, IL 60601', createdAt: '2024-03-13T09:00:00Z' },
        { id: '4', orderNumber: 'ORD-2024-004', customerName: 'Alice Brown', customerEmail: 'alice@example.com', items: [{ name: 'Bluetooth Speaker Mini', quantity: 1, price: 49.99 }], subtotal: 49.99, commission: 4.99, netAmount: 45.00, status: 'Delivered', shippingAddress: '321 Elm St, Houston, TX 77001', createdAt: '2024-03-12T14:00:00Z' }
      ]);
      this.loading.set(false);
    }, 500);
  }
  
  filteredOrders() {
    return this.orders().filter(o => !this.filterStatus || o.status === this.filterStatus);
  }
  
  getPendingCount(): number { return this.orders().filter(o => o.status === 'Pending').length; }
  getProcessingCount(): number { return this.orders().filter(o => o.status === 'Processing').length; }
  getShippedCount(): number { return this.orders().filter(o => o.status === 'Shipped').length; }
  getTotalRevenue(): number { return this.orders().reduce((sum, o) => sum + o.netAmount, 0); }
  
  viewOrder(order: VendorOrder) {
    this.selectedOrder.set(order);
    this.showDetailsModal.set(true);
  }
  
  processOrder(order: VendorOrder) {
    this.orders.update(orders => orders.map(o => o.id === order.id ? { ...o, status: 'Processing' } : o));
  }
  
  shipOrder(order: VendorOrder) {
    this.orders.update(orders => orders.map(o => o.id === order.id ? { ...o, status: 'Shipped' } : o));
  }
}
