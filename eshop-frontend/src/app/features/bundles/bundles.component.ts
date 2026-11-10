import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { BundlesService, BundleDto } from '@services/bundles.service';

@Component({
  selector: 'app-bundles',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent],
  templateUrl: './bundles.component.html',
  styleUrl: './bundles.component.css'
})
export class BundlesComponent implements OnInit {
  private readonly bundlesService = inject(BundlesService);
  
  bundles = signal<BundleDto[]>([]);
  featuredBundle = signal<BundleDto | null>(null);
  loading = signal(true);
  
  ngOnInit() {
    this.loadBundles();
  }
  
  loadBundles() {
    this.loading.set(true);
    
    this.bundlesService.getActiveBundles().subscribe({
      next: (bundles) => {
        this.bundles.set(bundles);
        if (bundles.length > 0) this.featuredBundle.set(bundles[0]);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        const mockBundles: BundleDto[] = [
          {
            id: '1', name: 'Home Office Essentials', description: 'Everything you need for a productive home office setup',
            status: 'Active', originalPrice: 299.99, bundlePrice: 249.99, discountPercentage: 17, currency: 'USD',
            items: [
              { productId: 'p1', productName: 'Ergonomic Keyboard', quantity: 1, originalPrice: 79.99 },
              { productId: 'p2', productName: 'Wireless Mouse', quantity: 1, originalPrice: 49.99 },
              { productId: 'p3', productName: 'Monitor Stand', quantity: 1, originalPrice: 89.99 },
              { productId: 'p4', productName: 'Desk Organizer', quantity: 1, originalPrice: 39.99 },
              { productId: 'p5', productName: 'LED Desk Lamp', quantity: 1, originalPrice: 39.99 }
            ],
            createdAt: new Date().toISOString()
          },
          {
            id: '2', name: 'Gaming Starter Pack', description: 'Level up your gaming experience with this essential bundle',
            status: 'Active', originalPrice: 449.99, bundlePrice: 379.99, discountPercentage: 15, currency: 'USD',
            items: [
              { productId: 'p6', productName: 'Gaming Headset', quantity: 1, originalPrice: 129.99 },
              { productId: 'p7', productName: 'Gaming Mouse', quantity: 1, originalPrice: 79.99 },
              { productId: 'p8', productName: 'RGB Mousepad', quantity: 1, originalPrice: 39.99 },
              { productId: 'p9', productName: 'Controller Stand', quantity: 1, originalPrice: 29.99 }
            ],
            createdAt: new Date().toISOString()
          },
          {
            id: '3', name: 'Fitness Bundle', description: 'Start your fitness journey with these essential items',
            status: 'Active', originalPrice: 199.99, bundlePrice: 159.99, discountPercentage: 20, currency: 'USD',
            items: [
              { productId: 'p10', productName: 'Yoga Mat', quantity: 1, originalPrice: 49.99 },
              { productId: 'p11', productName: 'Resistance Bands', quantity: 1, originalPrice: 29.99 },
              { productId: 'p12', productName: 'Water Bottle', quantity: 1, originalPrice: 24.99 }
            ],
            createdAt: new Date().toISOString()
          }
        ];
        this.bundles.set(mockBundles);
        this.featuredBundle.set(mockBundles[0]);
      }
    });
  }
  
  getSavingsPercent(bundle: BundleDto): number {
    return Math.round((1 - bundle.bundlePrice / bundle.originalPrice) * 100);
  }
}
