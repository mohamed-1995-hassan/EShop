import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface SalesReportDto {
  id: string;
  period: ReportPeriod;
  startDate: string;
  endDate: string;
  status: string;
  totalOrders: number;
  totalItems: number;
  uniqueCustomers: number;
  totalRevenue: number;
  totalDiscounts: number;
  netRevenue: number;
  averageOrderValue: number;
  conversionRate: number;
  currency: string;
}

export interface CustomerInsightDto {
  id: string;
  customerId: string;
  segment: CustomerSegment;
  totalOrders: number;
  lifetimeValue: number;
  averageOrderValue: number;
  daysSinceLastOrder: number;
  isAtRisk: boolean;
  currency: string;
}

export interface ProductPerformanceDto {
  id: string;
  productId: string;
  totalViews: number;
  totalAddedToCart: number;
  totalPurchases: number;
  totalReturns: number;
  viewToCartRate: number;
  cartToPurchaseRate: number;
  totalRevenue: number;
  isTopSeller: boolean;
  isSlowMover: boolean;
  currency: string;
}

export type ReportPeriod = 'Daily' | 'Weekly' | 'Monthly' | 'Quarterly' | 'Yearly';
export type CustomerSegment = 'New' | 'Active' | 'AtRisk' | 'Churned' | 'VIP' | 'Champion';

// Request DTOs
export interface CreateSalesReportRequest {
  period: ReportPeriod;
  startDate: string;
  endDate: string;
  currency?: string;
}

export interface CreateCustomerInsightRequest {
  customerId: string;
  currency?: string;
}

export interface CreateProductPerformanceRequest {
  productId: string;
  currency?: string;
}

export interface RecordPurchaseRequest {
  amount: number;
  currency: string;
}

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/analytics`;

  // === SALES REPORTS ===

  // GET /api/analytics/sales/{id}
  getSalesReport(id: string): Observable<SalesReportDto> {
    return this.http.get<SalesReportDto>(`${this.baseUrl}/sales/${id}`);
  }

  // GET /api/analytics/sales/recent
  getRecentSalesReports(count = 10): Observable<SalesReportDto[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<SalesReportDto[]>(`${this.baseUrl}/sales/recent`, { params });
  }

  // POST /api/analytics/sales/create
  createSalesReport(request: CreateSalesReportRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/sales/create`, request);
  }

  // POST /api/analytics/sales/{id}/complete
  completeSalesReport(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/sales/${id}/complete`, {});
  }

  // === CUSTOMER INSIGHTS ===

  // GET /api/analytics/customers/{id}
  getCustomerInsight(id: string): Observable<CustomerInsightDto> {
    return this.http.get<CustomerInsightDto>(`${this.baseUrl}/customers/${id}`);
  }

  // GET /api/analytics/customers/by-customer/{customerId}
  getCustomerInsightByCustomerId(customerId: string): Observable<CustomerInsightDto> {
    return this.http.get<CustomerInsightDto>(`${this.baseUrl}/customers/by-customer/${customerId}`);
  }

  // GET /api/analytics/customers/segment/{segment}
  getCustomersBySegment(segment: CustomerSegment): Observable<CustomerInsightDto[]> {
    return this.http.get<CustomerInsightDto[]>(`${this.baseUrl}/customers/segment/${segment}`);
  }

  // GET /api/analytics/customers/at-risk
  getAtRiskCustomers(): Observable<CustomerInsightDto[]> {
    return this.http.get<CustomerInsightDto[]>(`${this.baseUrl}/customers/at-risk`);
  }

  // GET /api/analytics/customers/top
  getTopCustomers(count = 10): Observable<CustomerInsightDto[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<CustomerInsightDto[]>(`${this.baseUrl}/customers/top`, { params });
  }

  // POST /api/analytics/customers/create
  createCustomerInsight(request: CreateCustomerInsightRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/customers/create`, request);
  }

  // === PRODUCT PERFORMANCE ===

  // GET /api/analytics/products/{id}
  getProductPerformance(id: string): Observable<ProductPerformanceDto> {
    return this.http.get<ProductPerformanceDto>(`${this.baseUrl}/products/${id}`);
  }

  // GET /api/analytics/products/by-product/{productId}
  getProductPerformanceByProductId(productId: string): Observable<ProductPerformanceDto> {
    return this.http.get<ProductPerformanceDto>(`${this.baseUrl}/products/by-product/${productId}`);
  }

  // GET /api/analytics/products/top-sellers
  getTopSellers(count = 10): Observable<ProductPerformanceDto[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<ProductPerformanceDto[]>(`${this.baseUrl}/products/top-sellers`, { params });
  }

  // GET /api/analytics/products/slow-movers
  getSlowMovers(count = 10): Observable<ProductPerformanceDto[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<ProductPerformanceDto[]>(`${this.baseUrl}/products/slow-movers`, { params });
  }

  // GET /api/analytics/products/low-stock
  getLowStockProducts(threshold = 10): Observable<ProductPerformanceDto[]> {
    const params = new HttpParams().set('threshold', threshold.toString());
    return this.http.get<ProductPerformanceDto[]>(`${this.baseUrl}/products/low-stock`, { params });
  }

  // POST /api/analytics/products/create
  createProductPerformance(request: CreateProductPerformanceRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/products/create`, request);
  }

  // POST /api/analytics/products/{id}/view
  recordProductView(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/products/${id}/view`, {});
  }

  // POST /api/analytics/products/{id}/add-to-cart
  recordAddToCart(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/products/${id}/add-to-cart`, {});
  }

  // POST /api/analytics/products/{id}/purchase
  recordProductPurchase(id: string, request: RecordPurchaseRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/products/${id}/purchase`, request);
  }

  // POST /api/analytics/products/{id}/return
  recordProductReturn(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/products/${id}/return`, {});
  }
}
