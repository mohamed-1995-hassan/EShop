import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { ProductsService, ProductDetailsDto, ProductFilterParams } from '@core/services/products.service';
import { CategoriesService, CategorySummaryDto } from '@core/services/categories.service';
import { CartService } from '@core/services/cart.service';
import { Product, Category } from '@shared/models';
import { forkJoin, catchError, of } from 'rxjs';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent, LoadingComponent],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css'
})
export class ProductListComponent implements OnInit {
  private productsService = inject(ProductsService);
  private categoriesService = inject(CategoriesService);
  private cartService = inject(CartService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  
  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  
  // Pagination
  currentPage = signal(1);
  pageSize = signal(12);
  totalProducts = signal(0);
  totalPages = computed(() => Math.ceil(this.totalProducts() / this.pageSize()));
  
  // Filters
  selectedCategories = signal<string[]>([]);
  minPrice: number | null = null;
  maxPrice: number | null = null;
  minRating = signal<number | null>(null);
  inStockOnly = false;
  sortBy = 'newest';
  viewMode = signal<'grid' | 'list'>('grid');
  
  pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: (number | string)[] = [];
    
    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push('...');
      for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
        pages.push(i);
      }
      if (current < total - 2) pages.push('...');
      pages.push(total);
    }
    return pages;
  });
  
  ngOnInit() {
    this.loadCategories();
    this.loadProducts();
  }
  
  loadCategories() {
    this.categoriesService.getRootCategories().pipe(
      catchError(err => {
        console.error('Error loading categories:', err);
        return of([]);
      })
    ).subscribe(categories => {
      this.categories.set(categories.map(cat => this.mapCategoryFromDto(cat)));
    });
  }
  
  loadProducts() {
    this.loading.set(true);
    this.error.set(null);
    
    const filters: ProductFilterParams = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      includeOutOfStock: !this.inStockOnly
    };
    
    // Apply category filter
    if (this.selectedCategories().length > 0) {
      filters.category = this.selectedCategories()[0];
    }
    
    // Apply price filters
    if (this.minPrice !== null && this.minPrice > 0) {
      filters.minPrice = this.minPrice;
    }
    if (this.maxPrice !== null && this.maxPrice > 0) {
      filters.maxPrice = this.maxPrice;
    }
    
    // Apply sorting
    switch (this.sortBy) {
      case 'price_asc':
        filters.sortBy = 'price';
        filters.descending = false;
        break;
      case 'price_desc':
        filters.sortBy = 'price';
        filters.descending = true;
        break;
      case 'name_asc':
        filters.sortBy = 'name';
        filters.descending = false;
        break;
      case 'rating':
        filters.sortBy = 'rating';
        filters.descending = true;
        break;
      case 'popular':
        filters.sortBy = 'sales';
        filters.descending = true;
        break;
      case 'newest':
      default:
        filters.sortBy = 'createdAt';
        filters.descending = true;
        break;
    }
    
    this.productsService.getProducts(filters).pipe(
      catchError(err => {
        console.error('Error loading products:', err);
        this.error.set('Failed to load products. Please try again.');
        return of({ items: [], totalCount: 0, page: 1, pageSize: 12, totalPages: 0 });
      })
    ).subscribe(result => {
      this.products.set(result.items.map(p => this.mapProductFromDto(p)));
      this.totalProducts.set(result.totalCount);
      this.loading.set(false);
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
  
  toggleCategory(categoryId: string) {
    this.selectedCategories.update(cats => {
      if (cats.includes(categoryId)) {
        return cats.filter(c => c !== categoryId);
      }
      return [...cats, categoryId];
    });
    this.currentPage.set(1);
    this.loadProducts();
  }
  
  applyPriceFilter() {
    this.currentPage.set(1);
    this.loadProducts();
  }
  
  setMinRating(rating: number) {
    this.minRating.set(rating);
    this.currentPage.set(1);
    this.loadProducts();
  }
  
  clearFilters() {
    this.selectedCategories.set([]);
    this.minPrice = null;
    this.maxPrice = null;
    this.minRating.set(null);
    this.inStockOnly = false;
    this.sortBy = 'newest';
    this.currentPage.set(1);
    this.loadProducts();
  }
  
  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadProducts();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
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
