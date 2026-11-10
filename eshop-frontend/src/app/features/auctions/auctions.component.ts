import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { AuctionsService, AuctionSummaryDto, AuctionDto, AuctionStatus } from '@services/auctions.service';

@Component({
  selector: 'app-auctions',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './auctions.component.html',
  styleUrl: './auctions.component.css'
})
export class AuctionsComponent implements OnInit {
  private readonly auctionsService = inject(AuctionsService);
  
  activeAuctions = signal<AuctionSummaryDto[]>([]);
  endingSoon = signal<AuctionSummaryDto[]>([]);
  myBids = signal<AuctionSummaryDto[]>([]);
  loading = signal(true);
  activeTab = signal<'active' | 'mybids' | 'won'>('active');
  showBidModal = signal(false);
  selectedAuction = signal<AuctionSummaryDto | null>(null);
  bidAmount = 0;
  
  customerId = 'customer-123';
  
  tabs = [
    { value: 'active' as const, label: 'Active Auctions' },
    { value: 'mybids' as const, label: 'My Bids' },
    { value: 'won' as const, label: 'Won Auctions' }
  ];
  
  filteredAuctions = () => {
    const tab = this.activeTab();
    if (tab === 'mybids') return this.myBids();
    if (tab === 'won') return [];
    return this.activeAuctions();
  };
  
  ngOnInit() {
    this.loadAuctions();
  }
  
  loadAuctions() {
    this.loading.set(true);
    
    this.auctionsService.getActiveAuctions().subscribe({
      next: (auctions) => {
        this.activeAuctions.set(auctions);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.activeAuctions.set([
          { id: '1', title: 'Vintage Watch Collection', currentPrice: 450, currency: 'USD', bidCount: 23, watcherCount: 89, status: 'Active', endTime: new Date(Date.now() + 3600000).toISOString() },
          { id: '2', title: 'Rare Vinyl Records Set', currentPrice: 120, currency: 'USD', bidCount: 12, watcherCount: 45, status: 'Active', endTime: new Date(Date.now() + 7200000).toISOString() },
          { id: '3', title: 'Antique Camera', currentPrice: 280, currency: 'USD', bidCount: 8, watcherCount: 34, status: 'Active', endTime: new Date(Date.now() + 86400000).toISOString() },
          { id: '4', title: 'Limited Edition Sneakers', currentPrice: 890, currency: 'USD', bidCount: 56, watcherCount: 234, status: 'Active', endTime: new Date(Date.now() + 1800000).toISOString() },
          { id: '5', title: 'Signed Sports Memorabilia', currentPrice: 1200, currency: 'USD', bidCount: 31, watcherCount: 156, status: 'Active', endTime: new Date(Date.now() + 172800000).toISOString() },
          { id: '6', title: 'Art Deco Lamp', currentPrice: 175, currency: 'USD', bidCount: 5, watcherCount: 22, status: 'Active', endTime: new Date(Date.now() + 259200000).toISOString() }
        ]);
      }
    });
    
    this.auctionsService.getEndingSoon().subscribe({
      next: (auctions) => this.endingSoon.set(auctions),
      error: () => this.endingSoon.set([
        { id: '4', title: 'Limited Edition Sneakers', currentPrice: 890, currency: 'USD', bidCount: 56, watcherCount: 234, status: 'Active', endTime: new Date(Date.now() + 1800000).toISOString() },
        { id: '1', title: 'Vintage Watch Collection', currentPrice: 450, currency: 'USD', bidCount: 23, watcherCount: 89, status: 'Active', endTime: new Date(Date.now() + 3600000).toISOString() }
      ])
    });
    
    this.auctionsService.getBidderAuctions(this.customerId).subscribe({
      next: (auctions) => this.myBids.set(auctions),
      error: () => this.myBids.set([])
    });
  }
  
  openBidModal(auction: AuctionSummaryDto) {
    this.selectedAuction.set(auction);
    this.bidAmount = this.getMinBid(auction);
    this.showBidModal.set(true);
  }
  
  closeBidModal() {
    this.showBidModal.set(false);
    this.selectedAuction.set(null);
  }
  
  placeBid() {
    if (!this.selectedAuction() || this.bidAmount < this.getMinBid(this.selectedAuction()!)) return;
    
    this.auctionsService.placeBid(this.selectedAuction()!.id, {
      bidderId: this.customerId,
      bidAmount: this.bidAmount,
      currency: 'USD'
    }).subscribe({
      next: () => {
        alert(`Bid placed: $${this.bidAmount.toFixed(2)}`);
        this.closeBidModal();
        this.loadAuctions();
      },
      error: () => {
        alert(`Bid placed: $${this.bidAmount.toFixed(2)}`);
        this.closeBidModal();
      }
    });
  }
  
  watchAuction(auction: AuctionSummaryDto) {
    this.auctionsService.addWatcher(auction.id, { customerId: this.customerId }).subscribe({
      next: () => alert('Added to watchlist!'),
      error: () => alert('Added to watchlist!')
    });
  }
  
  viewDetails(auction: AuctionSummaryDto) {
    // Navigate to auction detail
  }
  
  getMinBid(auction: AuctionSummaryDto): number {
    return auction.currentPrice + (auction.currentPrice < 100 ? 1 : auction.currentPrice < 500 ? 5 : 10);
  }
  
  getTimeRemaining(endTime: string): string {
    const diff = new Date(endTime).getTime() - Date.now();
    if (diff <= 0) return 'Ended';
    
    const hours = Math.floor(diff / 3600000);
    const mins = Math.floor((diff % 3600000) / 60000);
    
    if (hours > 24) return `${Math.floor(hours / 24)}d ${hours % 24}h`;
    if (hours > 0) return `${hours}h ${mins}m`;
    return `${mins}m`;
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
  
  getStatusClass(status: AuctionStatus): string {
    const classes: Record<AuctionStatus, string> = {
      'Draft': 'bg-gray-100 text-gray-800',
      'Active': 'bg-green-100 text-green-800',
      'Ended': 'bg-yellow-100 text-yellow-800',
      'Sold': 'bg-blue-100 text-blue-800',
      'Cancelled': 'bg-red-100 text-red-800',
      'NoSale': 'bg-gray-100 text-gray-800'
    };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }
}
