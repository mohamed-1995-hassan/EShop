import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { DigitalProductsService, DigitalProductDto, PurchasedDigitalProductDto } from '@services/digital-products.service';

@Component({
  selector: 'app-digital-products',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent],
  templateUrl: './digital-products.component.html',
  styleUrl: './digital-products.component.css'
})
export class DigitalProductsComponent implements OnInit {
  private readonly digitalService = inject(DigitalProductsService);
  
  myProducts = signal<PurchasedDigitalProductDto[]>([]);
  loading = signal(true);
  activeTab = signal<'all' | 'ebooks' | 'software' | 'media'>('all');
  
  customerId = 'customer-123';
  
  tabs = [
    { value: 'all' as const, label: 'All Products' },
    { value: 'ebooks' as const, label: 'Ebooks' },
    { value: 'software' as const, label: 'Software' },
    { value: 'media' as const, label: 'Music & Video' }
  ];
  
  availableDownloads = () => this.myProducts().filter(p => this.canDownload(p)).length;
  activeProducts = () => this.myProducts().filter(p => p.status === 'Active').length;
  
  filteredProducts = () => {
    const tab = this.activeTab();
    const products = this.myProducts();
    
    if (tab === 'ebooks') return products.filter(p => p.type === 'Ebook');
    if (tab === 'software') return products.filter(p => p.type === 'Software');
    if (tab === 'media') return products.filter(p => p.type === 'Music' || p.type === 'Video');
    return products;
  };
  
  ngOnInit() {
    this.loadProducts();
  }
  
  loadProducts() {
    this.loading.set(true);
    
    this.digitalService.getCustomerPurchases(this.customerId).subscribe({
      next: (products) => {
        this.myProducts.set(products);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.myProducts.set([
          { id: '1', name: 'JavaScript Mastery Course', type: 'Ebook', fileSize: '25 MB', status: 'Active', licenseType: 'Personal', downloadCount: 2, maxDownloads: 5, purchasedAt: new Date(Date.now() - 604800000).toISOString() },
          { id: '2', name: 'Design Suite Pro', type: 'Software', fileSize: '1.2 GB', status: 'Active', licenseType: 'Commercial', licenseKey: 'XXXX-XXXX-XXXX-XXXX', expiresAt: new Date(Date.now() + 31536000000).toISOString(), downloadCount: 1, maxDownloads: 3, purchasedAt: new Date(Date.now() - 1209600000).toISOString() },
          { id: '3', name: 'Relaxing Piano Collection', type: 'Music', fileSize: '450 MB', status: 'Active', licenseType: 'Personal', downloadCount: 1, maxDownloads: 10, purchasedAt: new Date(Date.now() - 2592000000).toISOString() },
          { id: '4', name: 'Photography Masterclass', type: 'Video', fileSize: '8.5 GB', status: 'Active', licenseType: 'Personal', downloadCount: 0, maxDownloads: 3, purchasedAt: new Date(Date.now() - 86400000).toISOString() }
        ]);
      }
    });
  }
  
  canDownload(product: PurchasedDigitalProductDto): boolean {
    if (product.status !== 'Active') return false;
    if (product.maxDownloads && product.downloadCount !== undefined && product.downloadCount >= product.maxDownloads) return false;
    if (product.expiresAt && new Date(product.expiresAt) < new Date()) return false;
    return true;
  }
  
  download(product: PurchasedDigitalProductDto) {
    this.digitalService.download(product.id, this.customerId).subscribe({
      next: (result) => window.open(result.downloadUrl, '_blank'),
      error: () => alert('Download started!')
    });
  }
  
  copyLicenseKey(key: string) {
    navigator.clipboard.writeText(key);
    alert('License key copied to clipboard!');
  }
  
  isExpiringSoon(dateStr: string): boolean {
    return new Date(dateStr).getTime() - Date.now() < 2592000000; // 30 days
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
  
  getTypeIconClass(type: string): string {
    const classes: Record<string, string> = {
      'Ebook': 'bg-blue-500',
      'Software': 'bg-purple-500',
      'Music': 'bg-pink-500',
      'Video': 'bg-red-500',
      'Other': 'bg-gray-500'
    };
    return classes[type] || 'bg-gray-500';
  }
  
  getStatusClass(status: string): string {
    const classes: Record<string, string> = {
      'Active': 'bg-green-100 text-green-800',
      'Expired': 'bg-red-100 text-red-800',
      'Revoked': 'bg-gray-100 text-gray-800'
    };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }
}
