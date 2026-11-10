import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface CategoryDto {
  id: string;
  name: string;
  slug: string;
  description?: string;
  imageUrl?: string;
  iconUrl?: string;
  parentId?: string;
  level: number;
  isActive: boolean;
  attributes: CategoryAttributeDto[];
  featuredProducts: string[];
  seo?: CategorySeoDto;
}

export interface CategorySummaryDto {
  id: string;
  name: string;
  slug: string;
  imageUrl?: string;
  productCount: number;
  isActive: boolean;
}

export interface CategoryAttributeDto {
  id: string;
  name: string;
  type: string;
  isFilterable: boolean;
  isSearchable: boolean;
}

export interface CategorySeoDto {
  metaTitle?: string;
  metaDescription?: string;
  metaKeywords?: string;
}

export interface CreateRootCategoryRequest {
  name: string;
  description?: string;
  imageUrl?: string;
}

export interface CreateChildCategoryRequest {
  name: string;
  parentId: string;
  level: number;
  description?: string;
}

export interface UpdateCategoryRequest {
  name: string;
  description?: string;
  imageUrl?: string;
  iconUrl?: string;
}

export interface AddAttributeRequest {
  name: string;
  type: 'Text' | 'Number' | 'Boolean' | 'Select' | 'MultiSelect';
  isFilterable?: boolean;
  isSearchable?: boolean;
}

export interface AddFeaturedProductRequest {
  productId: string;
}

export interface SetSeoRequest {
  metaTitle?: string;
  metaDescription?: string;
  metaKeywords?: string;
}

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/categories`;

  // GET /api/categories/{id} - Get category by ID
  getCategory(id: string): Observable<CategoryDto> {
    return this.http.get<CategoryDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/categories/slug/{slug} - Get category by slug
  getCategoryBySlug(slug: string): Observable<CategoryDto> {
    return this.http.get<CategoryDto>(`${this.baseUrl}/slug/${slug}`);
  }

  // GET /api/categories/root - Get root categories
  getRootCategories(): Observable<CategorySummaryDto[]> {
    return this.http.get<CategorySummaryDto[]>(`${this.baseUrl}/root`);
  }

  // GET /api/categories/{id}/children - Get child categories
  getChildCategories(parentId: string): Observable<CategorySummaryDto[]> {
    return this.http.get<CategorySummaryDto[]>(`${this.baseUrl}/${parentId}/children`);
  }

  // GET /api/categories/featured - Get featured categories
  getFeaturedCategories(): Observable<CategorySummaryDto[]> {
    return this.http.get<CategorySummaryDto[]>(`${this.baseUrl}/featured`);
  }

  // POST /api/categories/root - Create root category
  createRootCategory(request: CreateRootCategoryRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/root`, request);
  }

  // POST /api/categories/child - Create child category
  createChildCategory(request: CreateChildCategoryRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/child`, request);
  }

  // PUT /api/categories/{id} - Update category
  updateCategory(id: string, request: UpdateCategoryRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  // POST /api/categories/{id}/attributes - Add attribute
  addAttribute(categoryId: string, request: AddAttributeRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${categoryId}/attributes`, request);
  }

  // POST /api/categories/{id}/featured-products - Add featured product
  addFeaturedProduct(categoryId: string, request: AddFeaturedProductRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${categoryId}/featured-products`, request);
  }

  // PUT /api/categories/{id}/seo - Set SEO metadata
  setSeo(categoryId: string, request: SetSeoRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${categoryId}/seo`, request);
  }

  // POST /api/categories/{id}/activate - Activate category
  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/activate`, {});
  }

  // POST /api/categories/{id}/deactivate - Deactivate category
  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/deactivate`, {});
  }
}
