import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface StripeConfigDto {
  publishableKey: string;
  accountId?: string;
}

export interface PaymentIntentDto {
  clientSecret: string;
  paymentIntentId: string;
  amount: number;
  currency: string;
  status: string;
}

export interface CheckoutSessionDto {
  sessionId: string;
  url: string;
  expiresAt: string;
}

export interface CheckoutSessionStatusDto {
  sessionId: string;
  status: string;
  paymentStatus: string;
  customerEmail?: string;
  amountTotal: number;
  currency: string;
}

export interface StripeRefundDto {
  refundId: string;
  amount: number;
  currency: string;
  status: string;
  createdAt: string;
}

export interface StripeCustomerDto {
  customerId: string;
  email: string;
  name?: string;
}

export interface PaymentMethodDto {
  id: string;
  type: string;
  card?: CardDto;
  isDefault: boolean;
}

export interface CardDto {
  brand: string;
  last4: string;
  expMonth: number;
  expYear: number;
}

// Request DTOs
export interface CreatePaymentIntentRequest {
  amount: number;
  currency: string;
  customerId?: string;
  orderId?: string;
  description?: string;
  metadata?: { [key: string]: string };
  paymentMethodTypes?: string[];
  captureMethod?: 'automatic' | 'manual';
}

export interface CreateCheckoutSessionRequest {
  customerId?: string;
  customerEmail?: string;
  lineItems: CheckoutLineItem[];
  successUrl: string;
  cancelUrl: string;
  mode?: 'payment' | 'subscription' | 'setup';
  metadata?: { [key: string]: string };
  allowPromotionCodes?: boolean;
  shippingAddressCollection?: { allowedCountries: string[] };
}

export interface CheckoutLineItem {
  name: string;
  description?: string;
  imageUrl?: string;
  amount: number;
  currency: string;
  quantity: number;
}

export interface CreateRefundRequest {
  paymentIntentId: string;
  amount?: number;
  reason?: 'duplicate' | 'fraudulent' | 'requested_by_customer';
  metadata?: { [key: string]: string };
}

export interface CreateCustomerRequest {
  email: string;
  name?: string;
  phone?: string;
  metadata?: { [key: string]: string };
}

@Injectable({ providedIn: 'root' })
export class StripeService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/stripe`;

  // GET /api/stripe/config - Get Stripe publishable key
  getConfig(): Observable<StripeConfigDto> {
    return this.http.get<StripeConfigDto>(`${this.baseUrl}/config`);
  }

  // POST /api/stripe/payment-intent - Create payment intent
  createPaymentIntent(request: CreatePaymentIntentRequest): Observable<PaymentIntentDto> {
    return this.http.post<PaymentIntentDto>(`${this.baseUrl}/payment-intent`, request);
  }

  // POST /api/stripe/checkout-session - Create checkout session
  createCheckoutSession(request: CreateCheckoutSessionRequest): Observable<CheckoutSessionDto> {
    return this.http.post<CheckoutSessionDto>(`${this.baseUrl}/checkout-session`, request);
  }

  // GET /api/stripe/checkout-session/{sessionId} - Get checkout session status
  getCheckoutSessionStatus(sessionId: string): Observable<CheckoutSessionStatusDto> {
    return this.http.get<CheckoutSessionStatusDto>(`${this.baseUrl}/checkout-session/${sessionId}`);
  }

  // POST /api/stripe/refund - Create refund
  createRefund(request: CreateRefundRequest): Observable<StripeRefundDto> {
    return this.http.post<StripeRefundDto>(`${this.baseUrl}/refund`, request);
  }

  // POST /api/stripe/customer - Create Stripe customer
  createCustomer(request: CreateCustomerRequest): Observable<StripeCustomerDto> {
    return this.http.post<StripeCustomerDto>(`${this.baseUrl}/customer`, request);
  }

  // GET /api/stripe/customer/{stripeCustomerId}/payment-methods - Get customer payment methods
  getCustomerPaymentMethods(stripeCustomerId: string): Observable<PaymentMethodDto[]> {
    return this.http.get<PaymentMethodDto[]>(`${this.baseUrl}/customer/${stripeCustomerId}/payment-methods`);
  }
}
