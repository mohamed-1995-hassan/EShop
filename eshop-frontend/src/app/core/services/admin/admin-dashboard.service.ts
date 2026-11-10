import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

// DTOs
export interface DashboardOverviewDto {
  totalRevenue: number;
  revenueGrowth: number;
  totalOrders: number;
  ordersGrowth: number;
  totalCustomers: number;
  customersGrowth: number;
  averageOrderValue: number;
  aovGrowth: number;
  pendingOrders: number;
  lowStockProducts: number;
  currency: string;
}

export interface SalesDataDto {
  period: string;
  revenue: number;
  orders: number;
  averageOrderValue: number;
}

export interface TopProductDto {
  productId: string;
  productName: string;
  productImage?: string;
  quantitySold: number;
  revenue: number;
  currency: string;
}

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin/dashboard`;

  // GET /api/admin/dashboard/overview
  getOverview(): Observable<DashboardOverviewDto> {
    return this.http.get<DashboardOverviewDto>(`${this.baseUrl}/overview`);
  }

  // GET /api/admin/dashboard/sales
  getSalesData(period: 'daily' | 'weekly' | 'monthly' = 'daily', days = 30): Observable<SalesDataDto[]> {
    const params = new HttpParams()
      .set('period', period)
      .set('days', days.toString());
    return this.http.get<SalesDataDto[]>(`${this.baseUrl}/sales`, { params });
  }

  // GET /api/admin/dashboard/top-products
  getTopProducts(count = 10): Observable<TopProductDto[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<TopProductDto[]>(`${this.baseUrl}/top-products`, { params });
  }
}
