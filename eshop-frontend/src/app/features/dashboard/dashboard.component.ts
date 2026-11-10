import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { ProductsService, ProductDetailsDto } from '@core/services/products.service';
import { CategoriesService, CategorySummaryDto } from '@core/services/categories.service';
import { CartService } from '@core/services/cart.service';
import { Product, Category, Banner } from '@shared/models';
import { forkJoin, catchError, of } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private productsService = inject(ProductsService);
  private categoriesService = inject(CategoriesService);
  private cartService = inject(CartService);
  
  categories = signal<Category[]>([]);
  featuredProducts = signal<Product[]>([]);
  newArrivals = signal<Product[]>([]);
  banners = signal<Banner[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  
  ngOnInit() {
    this.loadData();
  }
  
  private loadData() {
    this.loading.set(true);
    this.error.set(null);
    
    forkJoin({
      categories: this.categoriesService.getRootCategories().pipe(
        catchError(err => {
          console.error('Error loading categories:', err);
          return of([]);
        })
      ),
      featured: this.productsService.getProducts({ pageSize: 4, sortBy: 'rating', descending: true }).pipe(
        catchError(err => {
          console.error('Error loading featured products:', err);
          return of({ items: [], totalCount: 0, page: 1, pageSize: 4, totalPages: 0 });
        })
      ),
      newArrivals: this.productsService.getProducts({ pageSize: 4, sortBy: 'createdAt', descending: true }).pipe(
        catchError(err => {
          console.error('Error loading new arrivals:', err);
          return of({ items: [], totalCount: 0, page: 1, pageSize: 4, totalPages: 0 });
        })
      )
    }).subscribe({
      next: ({ categories, featured, newArrivals }) => {
        // Map categories
        this.categories.set(categories.map(cat => this.mapCategoryFromDto(cat)));
        
        // Map featured products
        this.featuredProducts.set(featured.items.map(p => this.mapProductFromDto(p)));
        
        // Map new arrivals
        this.newArrivals.set(newArrivals.items.map(p => this.mapProductFromDto(p)));
        
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading dashboard data:', err);
        this.error.set('Failed to load data. Please try again.');
        this.loading.set(false);
      }
    });
  }
  
  private mapCategoryFromDto(dto: CategorySummaryDto): Category {
    return {
      id: dto.id,
      name: dto.name,
      slug: dto.slug,
      image: dto.imageUrl,
      productCount: dto.productCount,
      sortOrder: 0,
      isActive: dto.isActive
    };
  }
  
  private mapProductFromDto(dto: ProductDetailsDto): Product {
    return {
      id: dto.id,
      name: dto.name,
      slug: dto.sku.toLowerCase().replace(/\s+/g, '-'),
      description: dto.description,
      sku: dto.sku,
      price: dto.price,
      quantity: dto.stock,
      categoryId: dto.categories?.[0] || '',
      images: dto.images?.map((url, idx) => ({
        id: `${dto.id}-${idx}`,
        url,
        sortOrder: idx,
        isMain: idx === 0
      })) || [],
      mainImage: dto.images?.[0],
      isActive: dto.isActive,
      isFeatured: false,
      isDigital: false,
      createdAt: dto.createdAt,
      updatedAt: dto.updatedAt
    };
  }
  
  calculateDiscount(price: number, compareAtPrice: number): number {
    return Math.round((1 - price / compareAtPrice) * 100);
  }
  
  addToCart(product: Product) {
    this.cartService.quickAdd({
      id: product.id,
      name: product.name,
      price: product.price,
      image: product.mainImage,
      sku: product.sku
    });
  }
}
