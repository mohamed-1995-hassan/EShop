import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface ProductDetailsDto {
  id: string;
  sku: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  stock: number;
  lowStockThreshold: number;
  categories: string[];
  images: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface PagedProductsResult {
  items: ProductDetailsDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateProductRequest {
  sku: string;
  name: string;
  description: string;
  price: number;
  currency?: string;
  initialStock: number;
  lowStockThreshold: number;
  categories?: string[];
}

export interface CreateProductResult {
  success: boolean;
  productId?: string;
  errorMessage?: string;
}

export interface UpdateProductRequest {
  name: string;
  description: string;
  price?: number;
  currency?: string;
}

export interface AddStockRequest {
  quantity: number;
  reference?: string;
}

export interface AddStockResult {
  success: boolean;
  newStock?: number;
  errorMessage?: string;
}

export interface LowStockProductDto {
  id: string;
  sku: string;
  name: string;
  currentStock: number;
  lowStockThreshold: number;
}

export interface ProductFilterParams {
  page?: number;
  pageSize?: number;
  category?: string;
  minPrice?: number;
  maxPrice?: number;
  search?: string;
  includeOutOfStock?: boolean;
  sortBy?: string;
  descending?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/products`;

  // GET /api/products - Get paginated products with filtering
  getProducts(filters: ProductFilterParams = {}): Observable<PagedProductsResult> {
    let params = new HttpParams();
    if (filters.page) params = params.set('page', filters.page.toString());
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());
    if (filters.category) params = params.set('category', filters.category);
    if (filters.minPrice) params = params.set('minPrice', filters.minPrice.toString());
    if (filters.maxPrice) params = params.set('maxPrice', filters.maxPrice.toString());
    if (filters.search) params = params.set('search', filters.search);
    if (filters.includeOutOfStock !== undefined) params = params.set('includeOutOfStock', filters.includeOutOfStock.toString());
    if (filters.sortBy) params = params.set('sortBy', filters.sortBy);
    if (filters.descending !== undefined) params = params.set('descending', filters.descending.toString());

    return this.http.get<PagedProductsResult>(this.baseUrl, { params });
  }

  // GET /api/products/{id} - Get product by ID
  getProduct(id: string): Observable<ProductDetailsDto> {
    return this.http.get<ProductDetailsDto>(`${this.baseUrl}/${id}`);
  }

  // POST /api/products - Create new product
  createProduct(request: CreateProductRequest): Observable<CreateProductResult> {
    return this.http.post<CreateProductResult>(this.baseUrl, request);
  }

  // PUT /api/products/{id} - Update product
  updateProduct(id: string, request: UpdateProductRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  // POST /api/products/{id}/stock - Add stock
  addStock(id: string, request: AddStockRequest): Observable<AddStockResult> {
    return this.http.post<AddStockResult>(`${this.baseUrl}/${id}/stock`, request);
  }

  // POST /api/products/{id}/discontinue - Discontinue product
  discontinueProduct(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/discontinue`, {});
  }

  // GET /api/products/low-stock - Get low stock products
  getLowStockProducts(): Observable<LowStockProductDto[]> {
    return this.http.get<LowStockProductDto[]>(`${this.baseUrl}/low-stock`);
  }
}
