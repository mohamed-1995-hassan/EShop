import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

// DTOs
export interface AdminOrderDto {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  status: string;
  paymentStatus: string;
  total: number;
  currency: string;
  itemCount: number;
  createdAt: string;
}

export interface AdminOrderDetailDto extends AdminOrderDto {
  items: OrderItemDto[];
  shippingAddress: AddressDto;
  billingAddress?: AddressDto;
  subtotal: number;
  discount: number;
  tax: number;
  shipping: number;
  promoCode?: string;
  trackingNumber?: string;
  carrierName?: string;
  notes?: string;
  timeline: OrderTimelineEvent[];
}

export interface OrderItemDto {
  id: string;
  productId: string;
  productName: string;
  productImage?: string;
  sku: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface AddressDto {
  street: string;
  city: string;
  country: string;
  postalCode: string;
}

export interface OrderTimelineEvent {
  status: string;
  description: string;
  createdAt: string;
  createdBy?: string;
}

export interface PagedOrdersResult {
  items: AdminOrderDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface OrderFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  paymentStatus?: string;
  fromDate?: string;
  toDate?: string;
  sortBy?: string;
  descending?: boolean;
}

@Injectable({ providedIn: 'root' })
export class AdminOrdersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin/orders`;

  // GET /api/admin/orders - Get paginated orders
  getOrders(filters: OrderFilterParams = {}): Observable<PagedOrdersResult> {
    let params = new HttpParams();
    if (filters.page) params = params.set('page', filters.page.toString());
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());
    if (filters.search) params = params.set('search', filters.search);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.paymentStatus) params = params.set('paymentStatus', filters.paymentStatus);
    if (filters.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters.toDate) params = params.set('toDate', filters.toDate);
    if (filters.sortBy) params = params.set('sortBy', filters.sortBy);
    if (filters.descending !== undefined) params = params.set('descending', filters.descending.toString());

    return this.http.get<PagedOrdersResult>(this.baseUrl, { params });
  }

  // GET /api/admin/orders/{id} - Get order details
  getOrder(id: string): Observable<AdminOrderDetailDto> {
    return this.http.get<AdminOrderDetailDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/admin/orders/by-number/{orderNumber} - Get by order number
  getOrderByNumber(orderNumber: string): Observable<AdminOrderDetailDto> {
    return this.http.get<AdminOrderDetailDto>(`${this.baseUrl}/by-number/${orderNumber}`);
  }
}
