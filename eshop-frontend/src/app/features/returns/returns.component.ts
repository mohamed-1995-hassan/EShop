import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { ReturnsService, ReturnRequestDto, ReturnStatus } from '@services/returns.service';

@Component({
  selector: 'app-returns',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './returns.component.html',
  styleUrl: './returns.component.css'
})
export class ReturnsComponent implements OnInit {
  private readonly returnsService = inject(ReturnsService);
  
  // State
  returns = signal<ReturnRequestDto[]>([]);
  loading = signal(true);
  activeTab = signal<'all' | ReturnStatus>('all');
  showDetailsModal = signal(false);
  selectedReturn = signal<ReturnRequestDto | null>(null);
  
  // Mock customer ID
  customerId = 'customer-123';
  
  statusTabs = [
    { value: 'all' as const, label: 'All Returns' },
    { value: 'Draft' as const, label: 'Draft' },
    { value: 'Submitted' as const, label: 'Submitted' },
    { value: 'Approved' as const, label: 'Approved' },
    { value: 'Shipped' as const, label: 'Shipped' },
    { value: 'Completed' as const, label: 'Completed' }
  ];
  
  returnSteps = [
    { status: 'Submitted', label: 'Submitted' },
    { status: 'Approved', label: 'Approved' },
    { status: 'Shipped', label: 'Shipped' },
    { status: 'Received', label: 'Received' },
    { status: 'Completed', label: 'Refunded' }
  ];
  
  statusOrder: Record<string, number> = {
    'Draft': 0,
    'Submitted': 1,
    'Approved': 2,
    'Rejected': 2,
    'Shipped': 3,
    'Received': 4,
    'Inspecting': 4,
    'Completed': 5,
    'Cancelled': 0
  };
  
  filteredReturns = () => {
    const tab = this.activeTab();
    const all = this.returns();
    if (tab === 'all') return all;
    return all.filter(r => r.status === tab);
  };
  
  ngOnInit() {
    this.loadReturns();
  }
  
  loadReturns() {
    this.loading.set(true);
    this.returnsService.getCustomerReturns(this.customerId).subscribe({
      next: (returns) => {
        this.returns.set(returns);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        // Mock data for demo
        this.returns.set([
          {
            id: '1',
            returnNumber: 'RET-001',
            orderId: 'order-1',
            orderNumber: 'ORD-12345',
            customerId: this.customerId,
            status: 'Approved',
            items: [
              { id: 'item-1', orderItemId: 'oi-1', productId: 'p-1', productName: 'Wireless Headphones', quantity: 1, reason: 'Defective product', refundAmount: 79.99 }
            ],
            totalRefundAmount: 79.99,
            currency: 'USD',
            shippingLabel: { trackingNumber: '1Z999AA10123456784', carrier: 'UPS', labelUrl: '#' },
            images: [],
            createdAt: new Date(Date.now() - 172800000).toISOString(),
            submittedAt: new Date(Date.now() - 86400000).toISOString()
          },
          {
            id: '2',
            returnNumber: 'RET-002',
            orderId: 'order-2',
            orderNumber: 'ORD-12346',
            customerId: this.customerId,
            status: 'Completed',
            items: [
              { id: 'item-2', orderItemId: 'oi-2', productId: 'p-2', productName: 'Smart Watch', quantity: 1, reason: 'Changed mind', condition: 'New', refundAmount: 199.99 }
            ],
            totalRefundAmount: 199.99,
            currency: 'USD',
            images: [],
            createdAt: new Date(Date.now() - 604800000).toISOString(),
            submittedAt: new Date(Date.now() - 518400000).toISOString(),
            receivedAt: new Date(Date.now() - 345600000).toISOString(),
            completedAt: new Date(Date.now() - 259200000).toISOString()
          }
        ]);
      }
    });
  }
  
  getCountForTab(tab: string): number {
    if (tab === 'all') return this.returns().length;
    return this.returns().filter(r => r.status === tab).length;
  }
  
  viewDetails(returnReq: ReturnRequestDto) {
    this.selectedReturn.set(returnReq);
    this.showDetailsModal.set(true);
  }
  
  closeDetailsModal() {
    this.showDetailsModal.set(false);
    this.selectedReturn.set(null);
  }
  
  generateLabel(returnReq: ReturnRequestDto) {
    this.returnsService.generateLabel(returnReq.id, { carrier: 'UPS', weight: 1 }).subscribe({
      next: (result) => {
        const updated = { ...returnReq, shippingLabel: { trackingNumber: result.trackingNumber, carrier: 'UPS', labelUrl: result.labelUrl } };
        this.updateReturn(updated);
      },
      error: () => {
        const updated = { ...returnReq, shippingLabel: { trackingNumber: '1Z999AA10123456784', carrier: 'UPS', labelUrl: '#' } };
        this.updateReturn(updated);
      }
    });
  }
  
  markShipped(returnReq: ReturnRequestDto) {
    this.returnsService.markShipped(returnReq.id).subscribe({
      next: () => this.updateReturn({ ...returnReq, status: 'Shipped' as ReturnStatus }),
      error: () => this.updateReturn({ ...returnReq, status: 'Shipped' as ReturnStatus })
    });
  }
  
  submitReturn(returnReq: ReturnRequestDto) {
    this.returnsService.submit(returnReq.id).subscribe({
      next: () => this.updateReturn({ ...returnReq, status: 'Submitted' as ReturnStatus, submittedAt: new Date().toISOString() }),
      error: () => this.updateReturn({ ...returnReq, status: 'Submitted' as ReturnStatus, submittedAt: new Date().toISOString() })
    });
  }
  
  private updateReturn(updated: ReturnRequestDto) {
    const returns = this.returns();
    const index = returns.findIndex(r => r.id === updated.id);
    if (index >= 0) {
      returns[index] = updated;
      this.returns.set([...returns]);
    }
  }
  
  isStepCompleted(currentStatus: string, stepStatus: string): boolean {
    return this.statusOrder[currentStatus] >= this.statusOrder[stepStatus];
  }
  
  getStepClass(currentStatus: string, stepStatus: string): string {
    return this.isStepCompleted(currentStatus, stepStatus) ? 'bg-green-500 text-white' : 'bg-gray-200 text-gray-600';
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
  
  formatStatus(status: string): string {
    const map: Record<string, string> = { 'Draft': 'Draft', 'Submitted': 'Submitted', 'Approved': 'Approved', 'Rejected': 'Rejected', 'Shipped': 'Shipped', 'Received': 'Received', 'Inspecting': 'Under Inspection', 'Completed': 'Refunded', 'Cancelled': 'Cancelled' };
    return map[status] || status;
  }
  
  getStatusClass(status: string): string {
    const classes: Record<string, string> = { 'Draft': 'bg-gray-100 text-gray-800', 'Submitted': 'bg-blue-100 text-blue-800', 'Approved': 'bg-green-100 text-green-800', 'Rejected': 'bg-red-100 text-red-800', 'Shipped': 'bg-purple-100 text-purple-800', 'Received': 'bg-indigo-100 text-indigo-800', 'Inspecting': 'bg-yellow-100 text-yellow-800', 'Completed': 'bg-green-100 text-green-800', 'Cancelled': 'bg-gray-100 text-gray-800' };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }
}
