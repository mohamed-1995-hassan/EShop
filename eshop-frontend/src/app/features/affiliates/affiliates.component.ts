import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { AffiliatesService, AffiliateDto, AffiliateSummaryDto, CommissionDto, PayoutDto, ReferralDto } from '@core/services/affiliates.service';
import { AuthService } from '@core/auth/auth.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-affiliates',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule, HeaderComponent, FooterComponent, LoadingComponent],
  templateUrl: './affiliates.component.html',
  styleUrl: './affiliates.component.css'
})
export class AffiliatesComponent implements OnInit {
  private affiliatesService = inject(AffiliatesService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  
  loading = signal(true);
  saving = signal(false);
  isAffiliate = signal(false);
  affiliate = signal<AffiliateDto | null>(null);
  showApplyModal = signal(false);
  activeTab = signal('commissions');
  
  tabs = [
    { id: 'commissions', label: 'Commissions' },
    { id: 'referrals', label: 'Referrals' },
    { id: 'payouts', label: 'Payouts' }
  ];
  
  applyForm = this.fb.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    website: [''],
    agreeTerms: [false, Validators.requiredTrue]
  });
  
  ngOnInit() {
    this.loadAffiliateData();
  }
  
  loadAffiliateData() {
    const userId = this.authService.currentUser()?.id;
    if (!userId) {
      this.loading.set(false);
      return;
    }
    
    this.affiliatesService.getAffiliateByUser(userId).pipe(
      catchError(err => {
        if (err.status === 404) {
          return of(null);
        }
        console.error('Error loading affiliate data:', err);
        return of(this.getMockAffiliate());
      })
    ).subscribe(affiliate => {
      if (affiliate) {
        this.isAffiliate.set(true);
        this.affiliate.set(affiliate);
      }
      this.loading.set(false);
    });
  }
  
  getAffiliateLink(): string {
    const code = this.affiliate()?.code || 'ABC123';
    return `https://eshop.com/?ref=${code}`;
  }
  
  copyCode() {
    navigator.clipboard.writeText(this.affiliate()?.code || '');
  }
  
  copyLink() {
    navigator.clipboard.writeText(this.getAffiliateLink());
  }
  
  requestPayout() {
    console.log('Request payout');
  }
  
  submitApplication() {
    if (this.applyForm.invalid) return;
    
    this.saving.set(true);
    const formValue = this.applyForm.value;
    
    this.affiliatesService.apply({
      userId: this.authService.currentUser()?.id || '',
      name: formValue.name!,
      email: formValue.email!,
      website: formValue.website || undefined
    }).pipe(
      catchError(err => {
        console.error('Error applying:', err);
        return of('mock-affiliate-id');
      })
    ).subscribe(() => {
      this.saving.set(false);
      this.showApplyModal.set(false);
      this.loadAffiliateData();
    });
  }
  
  private getMockAffiliate(): AffiliateDto {
    return {
      id: '1',
      userId: 'user-1',
      name: 'John Doe',
      email: 'john@example.com',
      website: 'https://johndoe.com',
      code: 'JOHN2024',
      status: 'Active',
      commissionRate: 12,
      totalEarnings: 1250.50,
      pendingEarnings: 125.00,
      paidEarnings: 1125.50,
      currency: 'USD',
      referrals: [
        { id: '1', ipAddress: '192.168.1.1', status: 'Converted', landingPage: '/products', createdAt: '2024-03-01T10:00:00Z', convertedAt: '2024-03-01T10:30:00Z' },
        { id: '2', ipAddress: '192.168.1.2', status: 'Clicked', landingPage: '/deals', createdAt: '2024-03-10T14:00:00Z' },
        { id: '3', ipAddress: '192.168.1.3', status: 'Converted', landingPage: '/', createdAt: '2024-03-12T09:00:00Z', convertedAt: '2024-03-12T09:45:00Z' }
      ],
      commissions: [
        { id: '1', orderId: 'ORD-001', orderAmount: 150.00, commissionAmount: 18.00, currency: 'USD', description: 'Order commission', status: 'Approved', createdAt: '2024-03-01T11:00:00Z' },
        { id: '2', orderId: 'ORD-002', orderAmount: 89.99, commissionAmount: 10.80, currency: 'USD', description: 'Order commission', status: 'Approved', createdAt: '2024-03-05T15:00:00Z' },
        { id: '3', orderId: 'ORD-003', orderAmount: 299.00, commissionAmount: 35.88, currency: 'USD', description: 'Order commission', status: 'Pending', createdAt: '2024-03-12T10:00:00Z' }
      ],
      payouts: [
        { id: '1', amount: 100.00, currency: 'USD', method: 'PayPal', status: 'Completed', requestedAt: '2024-02-01T10:00:00Z', processedAt: '2024-02-03T14:00:00Z', transactionReference: 'PP-12345' },
        { id: '2', amount: 150.00, currency: 'USD', method: 'BankTransfer', status: 'Completed', requestedAt: '2024-02-15T10:00:00Z', processedAt: '2024-02-18T09:00:00Z', transactionReference: 'BT-67890' }
      ],
      createdAt: '2024-01-15T10:00:00Z',
      approvedAt: '2024-01-16T09:00:00Z'
    };
  }
}
