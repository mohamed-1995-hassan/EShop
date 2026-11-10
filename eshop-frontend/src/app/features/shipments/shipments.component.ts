import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { ShipmentsService, ShipmentDto, ShipmentStatus } from '@services/shipments.service';

@Component({
  selector: 'app-shipments',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './shipments.component.html',
  styleUrl: './shipments.component.css'
})
export class ShipmentsComponent implements OnInit {
  private readonly shipmentsService = inject(ShipmentsService);
  
  shipments = signal<ShipmentDto[]>([]);
  loading = signal(true);
  trackingNumber = '';
  expandedShipments = new Set<string>();
  
  customerId = 'customer-123';
  
  trackingSteps = [
    { status: 'Pending', label: 'Order Placed' },
    { status: 'Processing', label: 'Processing' },
    { status: 'Shipped', label: 'Shipped' },
    { status: 'InTransit', label: 'In Transit' },
    { status: 'Delivered', label: 'Delivered' }
  ];
  
  statusOrder: Record<string, number> = {
    'Pending': 0, 'Processing': 1, 'Shipped': 2, 'InTransit': 3, 'OutForDelivery': 4, 'Delivered': 5, 'Failed': 3, 'Returned': 3
  };
  
  ngOnInit() {
    this.loadShipments();
  }
  
  loadShipments() {
    this.loading.set(true);
    
    this.shipmentsService.getCustomerShipments(this.customerId).subscribe({
      next: (shipments) => {
        this.shipments.set(shipments);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.shipments.set([
          {
            id: '1', orderId: 'o1', orderNumber: 'ORD-12345', trackingNumber: '1Z999AA10123456784',
            carrier: 'UPS', serviceType: 'Ground', status: 'InTransit', weight: 2.5,
            estimatedDelivery: new Date(Date.now() + 172800000).toISOString(),
            address: { street: '123 Main St', city: 'New York', state: 'NY', postalCode: '10001', country: 'USA' },
            items: [{ productId: 'p1', productName: 'Wireless Headphones', quantity: 1 }],
            events: [
              { timestamp: new Date(Date.now() - 43200000).toISOString(), description: 'In transit to destination', location: 'Newark, NJ' },
              { timestamp: new Date(Date.now() - 86400000).toISOString(), description: 'Departed facility', location: 'Edison, NJ' },
              { timestamp: new Date(Date.now() - 129600000).toISOString(), description: 'Package picked up', location: 'Warehouse' }
            ],
            createdAt: new Date(Date.now() - 259200000).toISOString()
          },
          {
            id: '2', orderId: 'o2', orderNumber: 'ORD-12344', trackingNumber: '9400111899223033005033',
            carrier: 'USPS', serviceType: 'Priority Mail', status: 'OutForDelivery', weight: 1.2,
            estimatedDelivery: new Date().toISOString(),
            address: { street: '456 Oak Ave', city: 'Los Angeles', state: 'CA', postalCode: '90001', country: 'USA' },
            items: [{ productId: 'p2', productName: 'Phone Case', quantity: 2 }],
            events: [
              { timestamp: new Date(Date.now() - 7200000).toISOString(), description: 'Out for delivery', location: 'Los Angeles, CA' },
              { timestamp: new Date(Date.now() - 28800000).toISOString(), description: 'Arrived at local facility', location: 'Los Angeles, CA' }
            ],
            createdAt: new Date(Date.now() - 345600000).toISOString()
          }
        ]);
      }
    });
  }
  
  trackPackage() {
    if (!this.trackingNumber) return;
    
    this.shipmentsService.getByTrackingNumber(this.trackingNumber).subscribe({
      next: (shipment) => {
        if (!this.shipments().find(s => s.id === shipment.id)) {
          this.shipments.set([shipment, ...this.shipments()]);
        }
        this.trackingNumber = '';
      },
      error: () => alert('Package not found. Please check the tracking number.')
    });
  }
  
  toggleEvents(shipmentId: string) {
    if (this.expandedShipments.has(shipmentId)) {
      this.expandedShipments.delete(shipmentId);
    } else {
      this.expandedShipments.add(shipmentId);
    }
  }
  
  isStepCompleted(currentStatus: string, stepStatus: string): boolean {
    return this.statusOrder[currentStatus] >= this.statusOrder[stepStatus];
  }
  
  getStepClass(currentStatus: string, stepStatus: string): string {
    if (this.isStepCompleted(currentStatus, stepStatus)) {
      return 'bg-green-500 text-white';
    }
    return 'bg-gray-200 text-gray-500';
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
  }
  
  formatDateTime(dateStr: string): string {
    return new Date(dateStr).toLocaleString('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
  
  formatStatus(status: ShipmentStatus): string {
    const map: Record<string, string> = {
      'Pending': 'Pending', 'Processing': 'Processing', 'Shipped': 'Shipped',
      'InTransit': 'In Transit', 'OutForDelivery': 'Out for Delivery',
      'Delivered': 'Delivered', 'Failed': 'Delivery Failed', 'Returned': 'Returned'
    };
    return map[status] || status;
  }
  
  getStatusClass(status: ShipmentStatus): string {
    const classes: Record<string, string> = {
      'Pending': 'bg-gray-100 text-gray-800',
      'Processing': 'bg-yellow-100 text-yellow-800',
      'Shipped': 'bg-blue-100 text-blue-800',
      'InTransit': 'bg-purple-100 text-purple-800',
      'OutForDelivery': 'bg-indigo-100 text-indigo-800',
      'Delivered': 'bg-green-100 text-green-800',
      'Failed': 'bg-red-100 text-red-800',
      'Returned': 'bg-orange-100 text-orange-800'
    };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }
}
