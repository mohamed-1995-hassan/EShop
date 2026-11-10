import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface BundleDto {
  id: string;
  sku: string;
  name: string;
  description: string;
  type: BundleType;
  items: BundleItemDto[];
  discountType: DiscountType;
  discountValue: number;
  originalPrice: number;
  bundlePrice: number;
  imageUrl?: string;
  minItems?: number;
  maxItems?: number;
  requiredCount?: number;
  isActive: boolean;
  validFrom?: string;
  validUntil?: string;
  createdAt: string;
}

export interface BundleSummaryDto {
  id: string;
  sku: string;
  name: string;
  type: BundleType;
  itemCount: number;
  bundlePrice: number;
  savings: number;
  imageUrl?: string;
  isActive: boolean;
}

export interface BundleItemDto {
  id: string;
  productId: string;
  productName: string;
  productSku: string;
  unitPrice: number;
  currency: string;
  quantity: number;
  isRequired: boolean;
  minQuantity?: number;
  maxQuantity?: number;
}

export type BundleType = 'Fixed' | 'Customizable' | 'MixMatch';
export type DiscountType = 'Percentage' | 'FixedAmount' | 'FixedPrice';

export interface CreateFixedBundleRequest {
  sku: string;
  name: string;
  description: string;
  discountType: DiscountType;
  discountValue: number;
  imageUrl?: string;
}

export interface CreateCustomizableBundleRequest {
  sku: string;
  name: string;
  description: string;
  minItems: number;
  maxItems: number;
  discountType: DiscountType;
  discountValue: number;
  imageUrl?: string;
}

export interface CreateMixMatchBundleRequest {
  sku: string;
  name: string;
  description: string;
  requiredCount: number;
  discountType: DiscountType;
  discountValue: number;
  imageUrl?: string;
}

export interface AddBundleItemRequest {
  productId: string;
  productName: string;
  productSku: string;
  unitPrice: number;
  currency: string;
  quantity?: number;
  isRequired?: boolean;
  minQuantity?: number;
  maxQuantity?: number;
}

export interface UpdateDiscountRequest {
  type: DiscountType;
  value: number;
}

export interface SetValidityRequest {
  validFrom?: string;
  validUntil?: string;
}

export interface ValidateSelectionRequest {
  selectedQuantities: { [productId: string]: number };
}

export interface BundleValidationResult {
  isValid: boolean;
  errors: string[];
  totalPrice?: number;
  savings?: number;
}

export interface CalculatePriceRequest {
  selectedQuantities?: { [productId: string]: number };
}

@Injectable({ providedIn: 'root' })
export class BundlesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/bundles`;

  // GET /api/bundles/{id} - Get bundle by ID
  getBundle(id: string): Observable<BundleDto> {
    return this.http.get<BundleDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/bundles/sku/{sku} - Get bundle by SKU
  getBundleBySku(sku: string): Observable<BundleDto> {
    return this.http.get<BundleDto>(`${this.baseUrl}/sku/${sku}`);
  }

  // GET /api/bundles/active - Get active bundles
  getActiveBundles(): Observable<BundleSummaryDto[]> {
    return this.http.get<BundleSummaryDto[]>(`${this.baseUrl}/active`);
  }

  // GET /api/bundles/type/{type} - Get bundles by type
  getBundlesByType(type: BundleType): Observable<BundleSummaryDto[]> {
    return this.http.get<BundleSummaryDto[]>(`${this.baseUrl}/type/${type}`);
  }

  // GET /api/bundles/product/{productId} - Get bundles containing product
  getBundlesContainingProduct(productId: string): Observable<BundleSummaryDto[]> {
    return this.http.get<BundleSummaryDto[]>(`${this.baseUrl}/product/${productId}`);
  }

  // POST /api/bundles/fixed - Create fixed bundle
  createFixedBundle(request: CreateFixedBundleRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/fixed`, request);
  }

  // POST /api/bundles/customizable - Create customizable bundle
  createCustomizableBundle(request: CreateCustomizableBundleRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/customizable`, request);
  }

  // POST /api/bundles/mix-match - Create mix & match bundle
  createMixMatchBundle(request: CreateMixMatchBundleRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/mix-match`, request);
  }

  // POST /api/bundles/{bundleId}/items - Add item to bundle
  addItem(bundleId: string, request: AddBundleItemRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${bundleId}/items`, request);
  }

  // DELETE /api/bundles/{bundleId}/items/{itemId} - Remove item from bundle
  removeItem(bundleId: string, itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${bundleId}/items/${itemId}`);
  }

  // POST /api/bundles/{bundleId}/activate - Activate bundle
  activate(bundleId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${bundleId}/activate`, {});
  }

  // POST /api/bundles/{bundleId}/deactivate - Deactivate bundle
  deactivate(bundleId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${bundleId}/deactivate`, {});
  }

  // PUT /api/bundles/{bundleId}/discount - Update discount
  updateDiscount(bundleId: string, request: UpdateDiscountRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${bundleId}/discount`, request);
  }

  // PUT /api/bundles/{bundleId}/validity - Set validity period
  setValidity(bundleId: string, request: SetValidityRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${bundleId}/validity`, request);
  }

  // POST /api/bundles/{bundleId}/validate - Validate selection
  validateSelection(bundleId: string, request: ValidateSelectionRequest): Observable<BundleValidationResult> {
    return this.http.post<BundleValidationResult>(`${this.baseUrl}/${bundleId}/validate`, request);
  }

  // POST /api/bundles/{bundleId}/calculate-price - Calculate price
  calculatePrice(bundleId: string, request?: CalculatePriceRequest): Observable<number> {
    return this.http.post<number>(`${this.baseUrl}/${bundleId}/calculate-price`, request || {});
  }
}
