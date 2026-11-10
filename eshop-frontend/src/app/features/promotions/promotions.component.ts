import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { PromotionsService, PromotionDto, PromotionStatus } from '@services/promotions.service';

@Component({
  selector: 'app-promotions',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent],
  templateUrl: './promotions.component.html',
  styleUrl: './promotions.component.css'
})
export class PromotionsComponent implements OnInit {
  private readonly promotionsService = inject(PromotionsService);
  
  promotions = signal<PromotionDto[]>([]);
  loading = signal(true);
  activeFilter = signal<'all' | 'percent' | 'fixed' | 'shipping'>('all');
  
  Math = Math;
  
  filters = [
    { value: 'all' as const, label: 'All Deals' },
    { value: 'percent' as const, label: '% Off' },
    { value: 'fixed' as const, label: '$ Off' },
    { value: 'shipping' as const, label: 'Free Shipping' }
  ];
  
  activePromotions = () => this.promotions().filter(p => p.status === 'Active');
  
  filteredPromotions = () => {
    const filter = this.activeFilter();
    const promos = this.activePromotions();
    
    if (filter === 'percent') return promos.filter(p => p.type === 'PercentOff');
    if (filter === 'fixed') return promos.filter(p => p.type === 'FixedAmount');
    if (filter === 'shipping') return promos.filter(p => p.type === 'FreeShipping');
    return promos;
  };
  
  ngOnInit() {
    this.loadPromotions();
  }
  
  loadPromotions() {
    this.loading.set(true);
    
    this.promotionsService.getActivePromotions().subscribe({
      next: (promos) => {
        this.promotions.set(promos);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.promotions.set([
          {
            id: '1', name: 'Summer Sale', description: 'Get 30% off on all summer collection items!',
            type: 'PercentOff', discountValue: 30, status: 'Active',
            startDate: new Date(Date.now() - 86400000).toISOString(),
            endDate: new Date(Date.now() + 604800000).toISOString(),
            conditions: ['Applies to summer collection only', 'Cannot be combined with other offers'],
            maxUses: 1000, currentUses: 456, createdAt: new Date().toISOString()
          },
          {
            id: '2', name: 'Free Shipping Weekend', description: 'Free shipping on all orders this weekend!',
            type: 'FreeShipping', discountValue: 0, status: 'Active',
            startDate: new Date(Date.now() - 43200000).toISOString(),
            endDate: new Date(Date.now() + 172800000).toISOString(),
            conditions: ['No minimum purchase required', 'Continental US only'],
            currentUses: 234, createdAt: new Date().toISOString()
          },
          {
            id: '3', name: '$20 Off First Order', description: 'New customers get $20 off their first order!',
            type: 'FixedAmount', discountValue: 20, status: 'Active',
            conditions: ['First order only', 'Minimum purchase $50'],
            currentUses: 89, createdAt: new Date().toISOString()
          },
          {
            id: '4', name: 'Flash Sale - Electronics', description: 'Limited time 25% off all electronics!',
            type: 'PercentOff', discountValue: 25, status: 'Active',
            startDate: new Date(Date.now() - 3600000).toISOString(),
            endDate: new Date(Date.now() + 7200000).toISOString(),
            conditions: ['Electronics category only', 'While supplies last'],
            maxUses: 500, currentUses: 387, createdAt: new Date().toISOString()
          }
        ]);
      }
    });
  }
  
  getTimeRemaining(endDate: string): string {
    const diff = new Date(endDate).getTime() - Date.now();
    if (diff <= 0) return 'Ended';
    
    const days = Math.floor(diff / 86400000);
    const hours = Math.floor((diff % 86400000) / 3600000);
    
    if (days > 0) return `${days}d ${hours}h`;
    const mins = Math.floor((diff % 3600000) / 60000);
    if (hours > 0) return `${hours}h ${mins}m`;
    return `${mins}m`;
  }
  
  getPromoGradient(type: string): string {
    const gradients: Record<string, string> = {
      'PercentOff': 'bg-gradient-to-br from-purple-600 to-pink-600',
      'FixedAmount': 'bg-gradient-to-br from-green-600 to-teal-600',
      'FreeShipping': 'bg-gradient-to-br from-blue-600 to-cyan-600',
      'BuyOneGetOne': 'bg-gradient-to-br from-orange-600 to-red-600'
    };
    return gradients[type] || 'bg-gradient-to-br from-gray-600 to-gray-800';
  }
  
  getStatusClass(status: PromotionStatus): string {
    const classes: Record<PromotionStatus, string> = {
      'Draft': 'bg-gray-100 text-gray-800',
      'Scheduled': 'bg-blue-100 text-blue-800',
      'Active': 'bg-green-100 text-green-800',
      'Paused': 'bg-yellow-100 text-yellow-800',
      'Ended': 'bg-gray-100 text-gray-800',
      'Cancelled': 'bg-red-100 text-red-800'
    };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }
}
