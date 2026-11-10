import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { PaymentsService, PaymentDto, PaymentStatus } from '@services/payments.service';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './payments.component.html',
  styleUrl: './payments.component.css'
})
export class PaymentsComponent implements OnInit {
  private readonly paymentsService = inject(PaymentsService);
  
  payments = signal<PaymentDto[]>([]);
  loading = signal(true);
  searchQuery = '';
  statusFilter = '';
  methodFilter = '';
  
  customerId = 'customer-123';
  
  savedCards = [
    { id: '1', brand: 'Visa', last4: '4242', expMonth: '12', expYear: '26', isDefault: true },
    { id: '2', brand: 'Mastercard', last4: '8888', expMonth: '06', expYear: '25', isDefault: false }
  ];
  
  totalSuccessful = () => this.payments().filter(p => p.status === 'Completed').reduce((sum, p) => sum + p.amount, 0);
  pendingCount = () => this.payments().filter(p => p.status === 'Pending').length;
  totalRefunded = () => this.payments().filter(p => p.status === 'Refunded').reduce((sum, p) => sum + (p.refundedAmount || 0), 0);
  
  filteredPayments = () => {
    let result = this.payments();
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(p => p.transactionId.toLowerCase().includes(q) || p.orderNumber?.toLowerCase().includes(q));
    }
    if (this.statusFilter) result = result.filter(p => p.status === this.statusFilter);
    if (this.methodFilter) result = result.filter(p => p.method === this.methodFilter);
    return result;
  };
  
  ngOnInit() {
    this.loadPayments();
  }
  
  loadPayments() {
    this.loading.set(true);
    
    this.paymentsService.getCustomerPayments(this.customerId).subscribe({
      next: (payments) => {
        this.payments.set(payments);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.payments.set([
          { id: '1', orderId: 'o1', orderNumber: 'ORD-12345', transactionId: 'txn_1abc2def3ghi', amount: 149.99, currency: 'USD', method: 'CreditCard', cardLast4: '4242', status: 'Completed', createdAt: new Date(Date.now() - 86400000).toISOString() },
          { id: '2', orderId: 'o2', orderNumber: 'ORD-12346', transactionId: 'txn_4jkl5mno6pqr', amount: 89.99, currency: 'USD', method: 'PayPal', status: 'Completed', createdAt: new Date(Date.now() - 172800000).toISOString() },
          { id: '3', orderId: 'o3', orderNumber: 'ORD-12347', transactionId: 'txn_7stu8vwx9yz0', amount: 299.99, currency: 'USD', method: 'CreditCard', cardLast4: '8888', status: 'Pending', createdAt: new Date(Date.now() - 3600000).toISOString() },
          { id: '4', orderId: 'o4', orderNumber: 'ORD-12340', transactionId: 'txn_abc123def456', amount: 75.00, currency: 'USD', method: 'CreditCard', cardLast4: '4242', status: 'Refunded', refundedAmount: 75.00, createdAt: new Date(Date.now() - 604800000).toISOString() },
          { id: '5', orderId: 'o5', orderNumber: 'ORD-12341', transactionId: 'txn_ghi789jkl012', amount: 45.50, currency: 'USD', method: 'BankTransfer', status: 'Failed', createdAt: new Date(Date.now() - 259200000).toISOString() }
        ]);
      }
    });
  }
  
  viewDetails(payment: PaymentDto) {
    alert(`Payment Details:\nTransaction: ${payment.transactionId}\nAmount: ${payment.currency} ${payment.amount}\nStatus: ${payment.status}`);
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
  
  getStatusClass(status: PaymentStatus): string {
    const classes: Record<PaymentStatus, string> = {
      'Pending': 'bg-yellow-100 text-yellow-800',
      'Processing': 'bg-blue-100 text-blue-800',
      'Completed': 'bg-green-100 text-green-800',
      'Failed': 'bg-red-100 text-red-800',
      'Refunded': 'bg-purple-100 text-purple-800',
      'Cancelled': 'bg-gray-100 text-gray-800'
    };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }
}
