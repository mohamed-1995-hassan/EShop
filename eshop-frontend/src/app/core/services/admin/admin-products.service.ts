import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

// DTOs
export interface AdminProductDto {
  id: string;
  sku: string;
  name: string;
  price: number;
  currency: string;
  stock: number;
  lowStockThreshold: number;
  categoryNames: string[];
  status: string;
  isActive: boolean;
  imageUrl?: string;
  createdAt: string;
  updatedAt: string;
}

export interface AdminProductDetailDto extends AdminProductDto {
  description: string;
  images: string[];
  variants: ProductVariantDto[];
  salesCount: number;
  viewCount: number;
  reviewCount: number;
  averageRating?: number;
}

export interface ProductVariantDto {
  id: string;
  sku: string;
  name: string;
  price: number;
  stock: number;
  attributes: { [key: string]: string };
}

export interface PagedProductsResult {
  items: AdminProductDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ProductFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  category?: string;
  status?: string;
  isActive?: boolean;
  lowStock?: boolean;
  sortBy?: string;
  descending?: boolean;
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
  images?: string[];
}

export interface CreateProductResult {
  success: boolean;
  productId?: string;
  errorMessage?: string;
}

@Injectable({ providedIn: 'root' })
export class AdminProductsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin/products`;

  // GET /api/admin/products - Get paginated products
  getProducts(filters: ProductFilterParams = {}): Observable<PagedProductsResult> {
    let params = new HttpParams();
    if (filters.page) params = params.set('page', filters.page.toString());
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());
    if (filters.search) params = params.set('search', filters.search);
    if (filters.category) params = params.set('category', filters.category);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.isActive !== undefined) params = params.set('isActive', filters.isActive.toString());
    if (filters.lowStock !== undefined) params = params.set('lowStock', filters.lowStock.toString());
    if (filters.sortBy) params = params.set('sortBy', filters.sortBy);
    if (filters.descending !== undefined) params = params.set('descending', filters.descending.toString());

    return this.http.get<PagedProductsResult>(this.baseUrl, { params });
  }

  // GET /api/admin/products/{id} - Get product details
  getProduct(id: string): Observable<AdminProductDetailDto> {
    return this.http.get<AdminProductDetailDto>(`${this.baseUrl}/${id}`);
  }

  // POST /api/admin/products - Create product
  createProduct(request: CreateProductRequest): Observable<CreateProductResult> {
    return this.http.post<CreateProductResult>(this.baseUrl, request);
  }
}
