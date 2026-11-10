import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

// DTOs
export interface AdminCustomerDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone?: string;
  loyaltyTier: string;
  loyaltyPoints: number;
  totalOrders: number;
  totalSpent: number;
  currency: string;
  isActive: boolean;
  isVerified: boolean;
  createdAt: string;
  lastOrderAt?: string;
}

export interface AdminCustomerDetailDto extends AdminCustomerDto {
  dateOfBirth?: string;
  addresses: CustomerAddressDto[];
  recentOrders: RecentOrderDto[];
  walletBalance: number;
}

export interface CustomerAddressDto {
  id: string;
  label: string;
  street: string;
  city: string;
  country: string;
  postalCode: string;
  isDefault: boolean;
}

export interface RecentOrderDto {
  id: string;
  orderNumber: string;
  total: number;
  status: string;
  createdAt: string;
}

export interface PagedCustomersResult {
  items: AdminCustomerDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CustomerFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  loyaltyTier?: string;
  isActive?: boolean;
  sortBy?: string;
  descending?: boolean;
}

export interface UpdateCustomerRequest {
  firstName: string;
  lastName: string;
  phone?: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class AdminCustomersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin/customers`;

  // GET /api/admin/customers - Get paginated customers
  getCustomers(filters: CustomerFilterParams = {}): Observable<PagedCustomersResult> {
    let params = new HttpParams();
    if (filters.page) params = params.set('page', filters.page.toString());
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());
    if (filters.search) params = params.set('search', filters.search);
    if (filters.loyaltyTier) params = params.set('loyaltyTier', filters.loyaltyTier);
    if (filters.isActive !== undefined) params = params.set('isActive', filters.isActive.toString());
    if (filters.sortBy) params = params.set('sortBy', filters.sortBy);
    if (filters.descending !== undefined) params = params.set('descending', filters.descending.toString());

    return this.http.get<PagedCustomersResult>(this.baseUrl, { params });
  }

  // GET /api/admin/customers/{id} - Get customer details
  getCustomer(id: string): Observable<AdminCustomerDetailDto> {
    return this.http.get<AdminCustomerDetailDto>(`${this.baseUrl}/${id}`);
  }

  // PUT /api/admin/customers/{id} - Update customer
  updateCustomer(id: string, request: UpdateCustomerRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }
}
