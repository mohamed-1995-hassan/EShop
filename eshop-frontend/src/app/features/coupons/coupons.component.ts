import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { CouponsService, CouponSummaryDto, CouponDto } from '@services/coupons.service';

@Component({
  selector: 'app-coupons',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './coupons.component.html',
  styleUrl: './coupons.component.css'
})
export class CouponsComponent implements OnInit {
  private readonly couponsService = inject(CouponsService);
  
  coupons = signal<CouponSummaryDto[]>([]);
  expiringSoon = signal<CouponSummaryDto[]>([]);
  loading = signal(true);
  validating = signal(false);
  validationResult = signal<{ isValid: boolean; discountAmount?: number; errorMessage?: string } | null>(null);
  
  promoCode = '';
  customerId = 'customer-123';
  
  ngOnInit() {
    this.loadCoupons();
  }
  
  loadCoupons() {
    this.loading.set(true);
    
    this.couponsService.getActiveCoupons().subscribe({
      next: (coupons) => {
        this.coupons.set(coupons);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.coupons.set([
          { id: '1', code: 'SAVE20', name: '20% Off Everything', discountType: 'Percentage', discountValue: 20, validUntil: new Date(Date.now() + 604800000).toISOString(), usageCount: 150, maxUsage: 500 },
          { id: '2', code: 'FLAT10', name: '$10 Off Orders $50+', discountType: 'FixedAmount', discountValue: 10, validUntil: new Date(Date.now() + 1209600000).toISOString(), usageCount: 89, maxUsage: 200 },
          { id: '3', code: 'FREESHIP', name: 'Free Shipping', discountType: 'FreeShipping', discountValue: 0, usageCount: 320 },
          { id: '4', code: 'WELCOME15', name: '15% Off First Order', discountType: 'Percentage', discountValue: 15, usageCount: 45, maxUsage: 1000 },
          { id: '5', code: 'SUMMER25', name: 'Summer Sale 25% Off', discountType: 'Percentage', discountValue: 25, validUntil: new Date(Date.now() + 259200000).toISOString(), usageCount: 478, maxUsage: 500 }
        ]);
      }
    });
    
    this.couponsService.getExpiringSoonCoupons(7).subscribe({
      next: (coupons) => this.expiringSoon.set(coupons),
      error: () => this.expiringSoon.set([
        { id: '5', code: 'SUMMER25', name: 'Summer Sale', discountType: 'Percentage', discountValue: 25, validUntil: new Date(Date.now() + 259200000).toISOString(), usageCount: 478, maxUsage: 500 }
      ])
    });
  }
  
  validateCode() {
    if (!this.promoCode) return;
    
    this.validating.set(true);
    this.validationResult.set(null);
    
    this.couponsService.validateCoupon({
      code: this.promoCode,
      customerId: this.customerId,
      orderTotal: 100,
      currency: 'USD'
    }).subscribe({
      next: (result) => {
        this.validationResult.set(result);
        this.validating.set(false);
      },
      error: () => {
        this.validating.set(false);
        const code = this.promoCode.toUpperCase();
        if (['SAVE20', 'FLAT10', 'FREESHIP', 'WELCOME15', 'SUMMER25'].includes(code)) {
          this.validationResult.set({ isValid: true, discountAmount: code === 'SAVE20' ? 20 : code === 'FLAT10' ? 10 : 15 });
        } else {
          this.validationResult.set({ isValid: false, errorMessage: 'Invalid or expired coupon code' });
        }
      }
    });
  }
  
  copyCode(code: string) {
    navigator.clipboard.writeText(code);
    alert(`Copied: ${code}`);
  }
  
  getDiscountLabel(coupon: CouponSummaryDto): string {
    if (coupon.discountType === 'Percentage') return `${coupon.discountValue}% OFF`;
    if (coupon.discountType === 'FixedAmount') return `$${coupon.discountValue} OFF`;
    return 'FREE SHIPPING';
  }
  
  getDiscountTypeClass(type: string): string {
    const classes: Record<string, string> = {
      'Percentage': 'bg-purple-100 text-purple-800',
      'FixedAmount': 'bg-green-100 text-green-800',
      'FreeShipping': 'bg-blue-100 text-blue-800'
    };
    return classes[type] || 'bg-gray-100 text-gray-800';
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }
}
