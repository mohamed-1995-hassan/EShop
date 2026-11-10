import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { ProductsService, ProductDetailsDto, ProductFilterParams } from '@core/services/products.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './admin-products.component.html',
  styleUrl: './admin-products.component.css'
})
export class AdminProductsComponent implements OnInit {
  private productsService = inject(ProductsService);
  
  Math = Math;
  sidebarOpen = signal(true);
  products = signal<ProductDetailsDto[]>([]);
  loading = signal(true);
  
  searchTerm = '';
  sortBy = 'name';
  currentPage = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
  
  showCreateModal = signal(false);
  
  ngOnInit() {
    this.loadProducts();
  }
  
  loadProducts() {
    this.loading.set(true);
    
    const filters: ProductFilterParams = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      search: this.searchTerm || undefined,
      sortBy: this.sortBy,
      includeOutOfStock: true
    };
    
    this.productsService.getProducts(filters).pipe(
      catchError(err => {
        console.error('Error loading products:', err);
        return of({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 });
      })
    ).subscribe(result => {
      this.products.set(result.items);
      this.totalCount.set(result.totalCount);
      this.loading.set(false);
    });
  }
  
  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadProducts();
    }
  }
  
  discontinueProduct(id: string) {
    if (confirm('Are you sure you want to discontinue this product?')) {
      this.productsService.discontinueProduct(id).subscribe({
        next: () => this.loadProducts(),
        error: (err) => console.error('Error discontinuing product:', err)
      });
    }
  }
}
