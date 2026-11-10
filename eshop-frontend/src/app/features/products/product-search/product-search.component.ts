import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { ApiService } from '@core/services/api.service';
import { Product } from '@shared/models';

@Component({
  selector: 'app-product-search',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent, LoadingComponent],
  templateUrl: './product-search.component.html',
  styleUrl: './product-search.component.css'
})
export class ProductSearchComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  
  searchQuery = '';
  currentQuery = signal('');
  products = signal<Product[]>([]);
  loading = signal(false);
  totalResults = signal(0);
  
  popularSearches = ['Headphones', 'Laptop', 'Phone', 'Watch', 'Camera', 'Shoes'];
  
  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['q']) {
        this.searchQuery = params['q'];
        this.currentQuery.set(params['q']);
        this.performSearch();
      }
    });
  }
  
  search() {
    if (this.searchQuery.trim()) {
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { q: this.searchQuery },
        queryParamsHandling: 'merge'
      });
      this.currentQuery.set(this.searchQuery);
      this.performSearch();
    }
  }
  
  searchFor(term: string) {
    this.searchQuery = term;
    this.search();
  }
  
  private performSearch() {
    this.loading.set(true);
    
    // Mock search results
    setTimeout(() => {
      const query = this.currentQuery().toLowerCase();
      const allProducts: Product[] = [
        { id: '1', name: 'Premium Wireless Headphones', slug: 'premium-wireless-headphones', description: '', sku: 'WH-001', price: 149.99, quantity: 50, categoryId: '1', images: [], isActive: true, isFeatured: true, isDigital: false, mainImage: 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=300&h=300&fit=crop', createdAt: '', updatedAt: '' },
        { id: '2', name: 'Smart Fitness Watch', slug: 'smart-fitness-watch', description: '', sku: 'FW-001', price: 199.99, quantity: 30, categoryId: '1', images: [], isActive: true, isFeatured: true, isDigital: false, mainImage: 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=300&h=300&fit=crop', createdAt: '', updatedAt: '' },
        { id: '3', name: 'Designer Sunglasses', slug: 'designer-sunglasses', description: '', sku: 'SG-001', price: 89.99, quantity: 100, categoryId: '2', images: [], isActive: true, isFeatured: false, isDigital: false, mainImage: 'https://images.unsplash.com/photo-1572635196237-14b3f281503f?w=300&h=300&fit=crop', createdAt: '', updatedAt: '' }
      ];
      
      const filtered = allProducts.filter(p => 
        p.name.toLowerCase().includes(query) || 
        p.sku.toLowerCase().includes(query)
      );
      
      this.products.set(filtered);
      this.totalResults.set(filtered.length);
      this.loading.set(false);
    }, 500);
  }
}
