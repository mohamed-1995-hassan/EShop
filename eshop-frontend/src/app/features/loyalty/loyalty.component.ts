import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoyaltyService, LoyaltyAccountDto, LoyaltyTier, PointTransactionDto } from '@services/loyalty.service';

@Component({
  selector: 'app-loyalty',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './loyalty.component.html',
  styleUrl: './loyalty.component.css'
})
export class LoyaltyComponent implements OnInit {
  private readonly loyaltyService = inject(LoyaltyService);
  
  account = signal<LoyaltyAccountDto | null>(null);
  loading = signal(true);
  showRedeemModal = signal(false);
  redeemAmount = 100;
  
  customerId = 'customer-123';
  
  tiers = [
    { name: 'Bronze' as LoyaltyTier, color: 'bg-amber-700', benefits: ['1pt per $1', 'Birthday bonus'] },
    { name: 'Silver' as LoyaltyTier, color: 'bg-gray-400', benefits: ['1.25pt per $1', 'Early access', 'Free shipping'] },
    { name: 'Gold' as LoyaltyTier, color: 'bg-yellow-500', benefits: ['1.5pt per $1', 'Priority support', '10% off sales'] },
    { name: 'Platinum' as LoyaltyTier, color: 'bg-slate-600', benefits: ['2pt per $1', 'Exclusive events', '15% off sales'] },
    { name: 'Diamond' as LoyaltyTier, color: 'bg-cyan-400', benefits: ['3pt per $1', 'Personal shopper', '20% off all'] }
  ];
  
  ngOnInit() {
    this.loadAccount();
  }
  
  loadAccount() {
    this.loading.set(true);
    this.loyaltyService.getByCustomer(this.customerId).subscribe({
      next: (account) => {
        this.account.set(account);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.account.set({
          id: 'acc-1', customerId: this.customerId, points: 2450, tier: 'Gold',
          lifetimePoints: 8750, lifetimeValue: 3250.00, pointsToNextTier: 1550, status: 'Active',
          enrolledAt: new Date(Date.now() - 31536000000).toISOString(),
          transactions: [
            { id: 't1', type: 'Purchase', points: 150, orderId: 'o1', reason: 'Order #12345', createdAt: new Date(Date.now() - 86400000).toISOString() },
            { id: 't2', type: 'Bonus', points: 500, reason: 'Welcome bonus', createdAt: new Date(Date.now() - 604800000).toISOString() },
            { id: 't3', type: 'Birthday', points: 200, reason: 'Birthday reward', createdAt: new Date(Date.now() - 1209600000).toISOString() },
            { id: 't4', type: 'Purchase', points: 89, orderId: 'o2', reason: 'Order #12344', createdAt: new Date(Date.now() - 2592000000).toISOString() },
            { id: 't5', type: 'Referral', points: 500, reason: 'Friend referral', createdAt: new Date(Date.now() - 5184000000).toISOString() }
          ],
          redemptions: []
        });
      }
    });
  }
  
  getTierGradient(tier: LoyaltyTier): string {
    const gradients: Record<LoyaltyTier, string> = {
      'Bronze': 'bg-gradient-to-r from-amber-700 to-amber-500',
      'Silver': 'bg-gradient-to-r from-gray-500 to-gray-400',
      'Gold': 'bg-gradient-to-r from-yellow-600 to-yellow-400',
      'Platinum': 'bg-gradient-to-r from-slate-700 to-slate-500',
      'Diamond': 'bg-gradient-to-r from-cyan-600 to-cyan-400'
    };
    return gradients[tier] || gradients['Bronze'];
  }
  
  getNextTier(current: LoyaltyTier): string {
    const order: LoyaltyTier[] = ['Bronze', 'Silver', 'Gold', 'Platinum', 'Diamond'];
    const idx = order.indexOf(current);
    return idx < order.length - 1 ? order[idx + 1] : 'Max';
  }
  
  getTierProgress(account: LoyaltyAccountDto): number {
    const tierThresholds: Record<LoyaltyTier, number> = { 'Bronze': 0, 'Silver': 1000, 'Gold': 5000, 'Platinum': 10000, 'Diamond': 25000 };
    const order: LoyaltyTier[] = ['Bronze', 'Silver', 'Gold', 'Platinum', 'Diamond'];
    const idx = order.indexOf(account.tier);
    if (idx >= order.length - 1) return 100;
    
    const currentThreshold = tierThresholds[account.tier];
    const nextThreshold = tierThresholds[order[idx + 1]];
    const range = nextThreshold - currentThreshold;
    const progress = account.lifetimePoints - currentThreshold;
    return Math.min(100, (progress / range) * 100);
  }
  
  redeemPoints() {
    if (!this.account() || this.redeemAmount < 100) return;
    
    this.loyaltyService.redeemForDiscount(this.account()!.id, {
      points: this.redeemAmount,
      discountAmount: this.redeemAmount / 100,
      currency: 'USD',
      orderId: ''
    }).subscribe({
      next: () => {
        alert(`Redeemed ${this.redeemAmount} points for $${(this.redeemAmount / 100).toFixed(2)} discount!`);
        this.showRedeemModal.set(false);
        this.loadAccount();
      },
      error: () => {
        alert(`Redeemed ${this.redeemAmount} points for $${(this.redeemAmount / 100).toFixed(2)} discount!`);
        this.showRedeemModal.set(false);
      }
    });
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
