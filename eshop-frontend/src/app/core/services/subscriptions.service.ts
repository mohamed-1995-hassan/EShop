import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface SubscriptionDto {
  id: string;
  customerId: string;
  type: SubscriptionType;
  billingCycle: BillingCycle;
  status: SubscriptionStatus;
  items: SubscriptionItemDto[];
  shippingAddress: SubscriptionAddressDto;
  discountPercentage: number;
  billingDayOfMonth: number;
  nextBillingDate?: string;
  paymentMethodId?: string;
  totalAmount: number;
  currency: string;
  createdAt: string;
  activatedAt?: string;
  pausedUntil?: string;
  cancelledAt?: string;
}

export interface SubscriptionItemDto {
  id: string;
  productId: string;
  productName: string;
  unitPrice: number;
  currency: string;
  quantity: number;
  variant?: string;
}

export interface SubscriptionAddressDto {
  street: string;
  city: string;
  country: string;
  postalCode: string;
}

export type SubscriptionType = 'Replenishment' | 'Box' | 'Membership';
export type BillingCycle = 'Weekly' | 'BiWeekly' | 'Monthly' | 'Quarterly' | 'Yearly';
export type SubscriptionStatus = 'Draft' | 'Active' | 'Paused' | 'Cancelled' | 'Expired';

export interface CreateSubscriptionRequest {
  customerId: string;
  type: SubscriptionType;
  billingCycle: BillingCycle;
  street: string;
  city: string;
  country: string;
  postalCode: string;
  discountPercentage?: number;
  billingDayOfMonth?: number;
}

export interface AddSubscriptionItemRequest {
  productId: string;
  productName: string;
  unitPrice: number;
  currency: string;
  quantity: number;
  variant?: string;
}

export interface ActivateSubscriptionRequest {
  paymentMethodId: string;
}

export interface PauseSubscriptionRequest {
  until?: string;
  reason?: string;
}

export interface CancelSubscriptionRequest {
  reason: string;
  immediate?: boolean;
}

export interface SkipDeliveryRequest {
  reason?: string;
}

@Injectable({ providedIn: 'root' })
export class SubscriptionsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/subscriptions`;

  // GET /api/subscriptions/{id}
  getSubscription(id: string): Observable<SubscriptionDto> {
    return this.http.get<SubscriptionDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/subscriptions/customer/{customerId}
  getCustomerSubscriptions(customerId: string): Observable<SubscriptionDto[]> {
    return this.http.get<SubscriptionDto[]>(`${this.baseUrl}/customer/${customerId}`);
  }

  // GET /api/subscriptions/due-for-billing
  getDueForBilling(date?: string): Observable<SubscriptionDto[]> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<SubscriptionDto[]>(`${this.baseUrl}/due-for-billing`, { params });
  }

  // POST /api/subscriptions - Create subscription
  createSubscription(request: CreateSubscriptionRequest): Observable<string> {
    return this.http.post<string>(this.baseUrl, request);
  }

  // POST /api/subscriptions/{subscriptionId}/items - Add item
  addItem(subscriptionId: string, request: AddSubscriptionItemRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${subscriptionId}/items`, request);
  }

  // POST /api/subscriptions/{subscriptionId}/activate - Activate subscription
  activate(subscriptionId: string, request: ActivateSubscriptionRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${subscriptionId}/activate`, request);
  }

  // POST /api/subscriptions/{subscriptionId}/pause - Pause subscription
  pause(subscriptionId: string, request?: PauseSubscriptionRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${subscriptionId}/pause`, request || {});
  }

  // POST /api/subscriptions/{subscriptionId}/resume - Resume subscription
  resume(subscriptionId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${subscriptionId}/resume`, {});
  }

  // POST /api/subscriptions/{subscriptionId}/cancel - Cancel subscription
  cancel(subscriptionId: string, request: CancelSubscriptionRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${subscriptionId}/cancel`, request);
  }

  // POST /api/subscriptions/{subscriptionId}/skip-delivery - Skip next delivery
  skipDelivery(subscriptionId: string, request?: SkipDeliveryRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${subscriptionId}/skip-delivery`, request || {});
  }
}
