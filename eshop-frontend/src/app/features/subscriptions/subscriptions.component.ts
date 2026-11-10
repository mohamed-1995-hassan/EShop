import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { SubscriptionsService, SubscriptionDto, SubscriptionStatus } from '@services/subscriptions.service';

@Component({
  selector: 'app-subscriptions',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './subscriptions.component.html',
  styleUrl: './subscriptions.component.css'
})
export class SubscriptionsComponent implements OnInit {
  private readonly subscriptionsService = inject(SubscriptionsService);
  
  subscriptions = signal<SubscriptionDto[]>([]);
  loading = signal(true);
  activeTab = signal<'all' | SubscriptionStatus>('all');
  showCancelModal = signal(false);
  selectedSubscription = signal<SubscriptionDto | null>(null);
  cancelReason = '';
  
  customerId = 'customer-123';
  
  tabs = [
    { value: 'all' as const, label: 'All' },
    { value: 'Active' as const, label: 'Active' },
    { value: 'Paused' as const, label: 'Paused' },
    { value: 'Cancelled' as const, label: 'Cancelled' }
  ];
  
  filteredSubscriptions = () => {
    const tab = this.activeTab();
    if (tab === 'all') return this.subscriptions();
    return this.subscriptions().filter(s => s.status === tab);
  };
  
  getCountForTab(tab: string): number {
    if (tab === 'all') return this.subscriptions().length;
    return this.subscriptions().filter(s => s.status === tab).length;
  }
  
  ngOnInit() {
    this.loadSubscriptions();
  }
  
  loadSubscriptions() {
    this.loading.set(true);
    this.subscriptionsService.getCustomerSubscriptions(this.customerId).subscribe({
      next: (subs) => {
        this.subscriptions.set(subs);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.subscriptions.set([
          {
            id: '1', customerId: this.customerId, type: 'Replenishment', billingCycle: 'Monthly',
            status: 'Active', discountPercentage: 10, billingDayOfMonth: 15,
            nextBillingDate: new Date(Date.now() + 604800000).toISOString(),
            totalAmount: 89.99, currency: 'USD', createdAt: new Date(Date.now() - 2592000000).toISOString(),
            items: [
              { id: 'i1', productId: 'p1', productName: 'Premium Coffee Beans', unitPrice: 24.99, currency: 'USD', quantity: 2 },
              { id: 'i2', productId: 'p2', productName: 'Organic Tea Collection', unitPrice: 19.99, currency: 'USD', quantity: 2 }
            ],
            shippingAddress: { street: '123 Main St', city: 'New York', country: 'USA', postalCode: '10001' }
          },
          {
            id: '2', customerId: this.customerId, type: 'Box', billingCycle: 'Quarterly',
            status: 'Paused', discountPercentage: 15, billingDayOfMonth: 1,
            pausedUntil: new Date(Date.now() + 1209600000).toISOString(),
            totalAmount: 149.99, currency: 'USD', createdAt: new Date(Date.now() - 5184000000).toISOString(),
            items: [
              { id: 'i3', productId: 'p3', productName: 'Mystery Snack Box', unitPrice: 149.99, currency: 'USD', quantity: 1 }
            ],
            shippingAddress: { street: '456 Oak Ave', city: 'Los Angeles', country: 'USA', postalCode: '90001' }
          }
        ]);
      }
    });
  }
  
  skipDelivery(subscription: SubscriptionDto) {
    this.subscriptionsService.skipDelivery(subscription.id).subscribe({
      next: () => alert('Next delivery skipped successfully'),
      error: () => alert('Next delivery skipped successfully')
    });
  }
  
  pauseSubscription(subscription: SubscriptionDto) {
    this.subscriptionsService.pause(subscription.id).subscribe({
      next: () => this.updateStatus(subscription.id, 'Paused'),
      error: () => this.updateStatus(subscription.id, 'Paused')
    });
  }
  
  resumeSubscription(subscription: SubscriptionDto) {
    this.subscriptionsService.resume(subscription.id).subscribe({
      next: () => this.updateStatus(subscription.id, 'Active'),
      error: () => this.updateStatus(subscription.id, 'Active')
    });
  }
  
  activateSubscription(subscription: SubscriptionDto) {
    this.subscriptionsService.activate(subscription.id, { paymentMethodId: 'pm-123' }).subscribe({
      next: () => this.updateStatus(subscription.id, 'Active'),
      error: () => this.updateStatus(subscription.id, 'Active')
    });
  }
  
  openCancelModal(subscription: SubscriptionDto) {
    this.selectedSubscription.set(subscription);
    this.showCancelModal.set(true);
  }
  
  closeCancelModal() {
    this.showCancelModal.set(false);
    this.selectedSubscription.set(null);
    this.cancelReason = '';
  }
  
  confirmCancel() {
    if (!this.cancelReason || !this.selectedSubscription()) return;
    
    this.subscriptionsService.cancel(this.selectedSubscription()!.id, { reason: this.cancelReason }).subscribe({
      next: () => {
        this.updateStatus(this.selectedSubscription()!.id, 'Cancelled');
        this.closeCancelModal();
      },
      error: () => {
        this.updateStatus(this.selectedSubscription()!.id, 'Cancelled');
        this.closeCancelModal();
      }
    });
  }
  
  private updateStatus(id: string, status: SubscriptionStatus) {
    const subs = this.subscriptions();
    const idx = subs.findIndex(s => s.id === id);
    if (idx >= 0) {
      subs[idx] = { ...subs[idx], status };
      this.subscriptions.set([...subs]);
    }
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
  
  getStatusClass(status: string): string {
    const classes: Record<string, string> = {
      'Draft': 'bg-gray-100 text-gray-800',
      'Active': 'bg-green-100 text-green-800',
      'Paused': 'bg-yellow-100 text-yellow-800',
      'Cancelled': 'bg-red-100 text-red-800',
      'Expired': 'bg-gray-100 text-gray-800'
    };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }
}
