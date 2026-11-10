import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface CouponDto {
  id: string;
  code: string;
  name: string;
  discountType: 'Percentage' | 'FixedAmount' | 'FreeShipping';
  discountValue: number;
  maxDiscount?: number;
  minimumOrderAmount?: number;
  minimumOrderCurrency?: string;
  maxUsage?: number;
  usageCount: number;
  validFrom?: string;
  validUntil?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CouponSummaryDto {
  id: string;
  code: string;
  name: string;
  discountType: string;
  discountValue: number;
  validUntil?: string;
  usageCount: number;
  maxUsage?: number;
}

export interface CouponValidationResult {
  isValid: boolean;
  discountAmount?: number;
  errorMessage?: string;
}

export interface ValidateCouponRequest {
  code: string;
  customerId: string;
  orderTotal: number;
  currency: string;
  isFirstOrder?: boolean;
}

export interface CreatePercentageCouponRequest {
  code: string;
  name: string;
  percentage: number;
  maxDiscount?: number;
  maxUsage?: number;
}

export interface CreateFixedAmountCouponRequest {
  code: string;
  name: string;
  amount: number;
  currency: string;
  maxUsage?: number;
}

export interface CreateFreeShippingCouponRequest {
  code: string;
  name: string;
  maxUsage?: number;
}

export interface GenerateBulkCouponsRequest {
  prefix: string;
  name: string;
  discountType: 'Percentage' | 'FixedAmount' | 'FreeShipping';
  discountValue: number;
  quantity: number;
  usagePerCoupon?: number;
}

export interface ApplyCouponRequest {
  orderId: string;
  customerId: string;
  discountApplied: number;
  currency: string;
}

export interface SetValidityRequest {
  validFrom?: string;
  validUntil?: string;
}

export interface SetMinOrderRequest {
  minimumAmount: number;
  currency: string;
}

@Injectable({ providedIn: 'root' })
export class CouponsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/coupons`;

  // GET /api/coupons/{id} - Get coupon by ID
  getCoupon(id: string): Observable<CouponDto> {
    return this.http.get<CouponDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/coupons/code/{code} - Get coupon by code
  getCouponByCode(code: string): Observable<CouponDto> {
    return this.http.get<CouponDto>(`${this.baseUrl}/code/${code}`);
  }

  // GET /api/coupons/active - Get active coupons
  getActiveCoupons(): Observable<CouponSummaryDto[]> {
    return this.http.get<CouponSummaryDto[]>(`${this.baseUrl}/active`);
  }

  // GET /api/coupons/expiring-soon - Get expiring coupons
  getExpiringSoonCoupons(withinDays = 7): Observable<CouponSummaryDto[]> {
    const params = new HttpParams().set('withinDays', withinDays.toString());
    return this.http.get<CouponSummaryDto[]>(`${this.baseUrl}/expiring-soon`, { params });
  }

  // POST /api/coupons/validate - Validate coupon
  validateCoupon(request: ValidateCouponRequest): Observable<CouponValidationResult> {
    return this.http.post<CouponValidationResult>(`${this.baseUrl}/validate`, request);
  }

  // POST /api/coupons/percentage - Create percentage coupon
  createPercentageCoupon(request: CreatePercentageCouponRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/percentage`, request);
  }

  // POST /api/coupons/fixed-amount - Create fixed amount coupon
  createFixedAmountCoupon(request: CreateFixedAmountCouponRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/fixed-amount`, request);
  }

  // POST /api/coupons/free-shipping - Create free shipping coupon
  createFreeShippingCoupon(request: CreateFreeShippingCouponRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/free-shipping`, request);
  }

  // POST /api/coupons/bulk - Generate bulk coupons
  generateBulkCoupons(request: GenerateBulkCouponsRequest): Observable<string[]> {
    return this.http.post<string[]>(`${this.baseUrl}/bulk`, request);
  }

  // POST /api/coupons/{id}/apply - Apply coupon
  applyCoupon(id: string, request: ApplyCouponRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${id}/apply`, request);
  }

  // POST /api/coupons/{id}/activate - Activate coupon
  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/activate`, {});
  }

  // POST /api/coupons/{id}/deactivate - Deactivate coupon
  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/deactivate`, {});
  }

  // PUT /api/coupons/{id}/validity - Set validity period
  setValidity(id: string, request: SetValidityRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/validity`, request);
  }

  // PUT /api/coupons/{id}/min-order - Set minimum order
  setMinOrder(id: string, request: SetMinOrderRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/min-order`, request);
  }
}
