import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface PromotionDto {
  id: string;
  name: string;
  description?: string;
  type: PromotionType;
  discountType: DiscountType;
  discountValue: number;
  priority: number;
  conditions: PromotionConditionDto[];
  applicableProducts: string[];
  status: PromotionStatus;
  usageCount: number;
  maxUsage?: number;
  validFrom: string;
  validUntil: string;
  createdAt: string;
}

export interface PromotionConditionDto {
  type: string;
  value: any;
}

export type PromotionType = 'Percentage' | 'Fixed' | 'FreeShipping' | 'BuyXGetY';
export type DiscountType = 'Percentage' | 'FixedAmount';
export type PromotionStatus = 'Draft' | 'Active' | 'Paused' | 'Expired' | 'Cancelled';

export interface PromotionValidationResult {
  isApplicable: boolean;
  discountAmount?: number;
  errorMessage?: string;
}

export interface ValidatePromotionRequest {
  customerId: string;
  productIds: string[];
  orderTotal: number;
  currency: string;
}

export interface CreatePercentagePromotionRequest {
  name: string;
  description?: string;
  percentage: number;
  maxDiscount?: number;
  validFrom: string;
  validUntil: string;
  priority?: number;
  maxUsage?: number;
}

export interface CreateFixedPromotionRequest {
  name: string;
  description?: string;
  amount: number;
  currency: string;
  validFrom: string;
  validUntil: string;
  priority?: number;
  maxUsage?: number;
}

export interface CreateFreeShippingPromotionRequest {
  name: string;
  description?: string;
  minOrderAmount?: number;
  currency?: string;
  validFrom: string;
  validUntil: string;
  priority?: number;
}

export interface ExtendPromotionRequest {
  newEndDate: string;
}

export interface RecordUsageRequest {
  orderId: string;
  customerId: string;
  discountApplied: number;
  currency: string;
}

@Injectable({ providedIn: 'root' })
export class PromotionsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/promotions`;

  // GET /api/promotions/{id}
  getPromotion(id: string): Observable<PromotionDto> {
    return this.http.get<PromotionDto>(`${this.baseUrl}/${id}`);
  }

  // POST /api/promotions/validate
  validatePromotion(request: ValidatePromotionRequest): Observable<PromotionValidationResult> {
    return this.http.post<PromotionValidationResult>(`${this.baseUrl}/validate`, request);
  }

  // POST /api/promotions/percentage
  createPercentagePromotion(request: CreatePercentagePromotionRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/percentage`, request);
  }

  // POST /api/promotions/fixed
  createFixedPromotion(request: CreateFixedPromotionRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/fixed`, request);
  }

  // POST /api/promotions/free-shipping
  createFreeShippingPromotion(request: CreateFreeShippingPromotionRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/free-shipping`, request);
  }

  // POST /api/promotions/{id}/activate
  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/activate`, {});
  }

  // POST /api/promotions/{id}/deactivate
  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/deactivate`, {});
  }

  // POST /api/promotions/{id}/extend
  extend(id: string, request: ExtendPromotionRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/extend`, request);
  }

  // POST /api/promotions/{id}/products/{productId} - Add applicable product
  addApplicableProduct(promotionId: string, productId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${promotionId}/products/${productId}`, {});
  }

  // POST /api/promotions/{id}/usage - Record usage
  recordUsage(promotionId: string, request: RecordUsageRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${promotionId}/usage`, request);
  }

  // GET /api/promotions/active
  getActivePromotions(): Observable<PromotionDto[]> {
    return this.http.get<PromotionDto[]>(`${this.baseUrl}/active`);
  }

  // GET /api/promotions/type/{type}
  getPromotionsByType(type: PromotionType): Observable<PromotionDto[]> {
    return this.http.get<PromotionDto[]>(`${this.baseUrl}/type/${type}`);
  }

  // GET /api/promotions/upcoming
  getUpcomingPromotions(): Observable<PromotionDto[]> {
    return this.http.get<PromotionDto[]>(`${this.baseUrl}/upcoming`);
  }

  // GET /api/promotions/product/{productId}
  getPromotionsForProduct(productId: string): Observable<PromotionDto[]> {
    return this.http.get<PromotionDto[]>(`${this.baseUrl}/product/${productId}`);
  }
}
