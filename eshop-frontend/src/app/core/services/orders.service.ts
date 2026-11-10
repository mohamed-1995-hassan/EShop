import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface OrderDetailsDto {
  id: string;
  orderNumber: string;
  customerId: string;
  status: string;
  items: OrderItemDto[];
  shippingAddress: AddressDto;
  subtotal: number;
  discount: number;
  tax: number;
  shipping: number;
  total: number;
  currency: string;
  trackingNumber?: string;
  carrierName?: string;
  promoCode?: string;
  loyaltyPointsRedeemed?: number;
  createdAt: string;
  updatedAt: string;
}

export interface OrderSummaryDto {
  id: string;
  orderNumber: string;
  status: string;
  total: number;
  currency: string;
  itemCount: number;
  createdAt: string;
}

export interface OrderItemDto {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface AddressDto {
  street: string;
  city: string;
  country: string;
  postalCode: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateOrderRequest {
  customerId: string;
  shippingAddress: AddressDto;
  items: OrderItemRequest[];
  promoCode?: string;
  loyaltyPointsToRedeem?: number;
}

export interface OrderItemRequest {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface CreateOrderResult {
  success: boolean;
  orderId?: string;
  orderNumber?: string;
  errorMessage?: string;
}

export interface CancelOrderRequest {
  reason: string;
}

export interface CancelOrderResult {
  success: boolean;
  errorMessage?: string;
}

export interface ShipOrderRequest {
  trackingNumber: string;
  carrierName: string;
}

export interface ShipOrderResult {
  success: boolean;
  errorMessage?: string;
}

export interface AddOrderItemRequest {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface ModifyOrderResult {
  success: boolean;
  errorMessage?: string;
}

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/orders`;

  // GET /api/orders/{id} - Get order by ID
  getOrder(id: string): Observable<OrderDetailsDto> {
    return this.http.get<OrderDetailsDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/orders/customer/{customerId} - Get customer orders
  getCustomerOrders(customerId: string, page = 1, pageSize = 20): Observable<PagedResult<OrderSummaryDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<OrderSummaryDto>>(`${this.baseUrl}/customer/${customerId}`, { params });
  }

  // POST /api/orders - Create new order
  createOrder(request: CreateOrderRequest): Observable<CreateOrderResult> {
    return this.http.post<CreateOrderResult>(this.baseUrl, request);
  }

  // POST /api/orders/{id}/cancel - Cancel order
  cancelOrder(id: string, request: CancelOrderRequest): Observable<CancelOrderResult> {
    return this.http.post<CancelOrderResult>(`${this.baseUrl}/${id}/cancel`, request);
  }

  // POST /api/orders/{id}/ship - Ship order
  shipOrder(id: string, request: ShipOrderRequest): Observable<ShipOrderResult> {
    return this.http.post<ShipOrderResult>(`${this.baseUrl}/${id}/ship`, request);
  }

  // POST /api/orders/{id}/deliver - Confirm delivery
  deliverOrder(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/deliver`, {});
  }

  // POST /api/orders/{id}/items - Add item to order
  addOrderItem(orderId: string, request: AddOrderItemRequest): Observable<ModifyOrderResult> {
    return this.http.post<ModifyOrderResult>(`${this.baseUrl}/${orderId}/items`, request);
  }
}
