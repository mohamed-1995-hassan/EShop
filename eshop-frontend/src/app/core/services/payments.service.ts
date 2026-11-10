import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface PaymentDetailsDto {
  id: string;
  orderId: string;
  customerId: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  paymentMethod: string;
  gatewayTransactionId?: string;
  refunds: RefundDto[];
  createdAt: string;
  authorizedAt?: string;
  capturedAt?: string;
}

export type PaymentStatus = 'Pending' | 'Authorized' | 'Captured' | 'Refunded' | 'PartiallyRefunded' | 'Voided' | 'Failed';

export interface RefundDto {
  id: string;
  amount: number;
  reason: string;
  status: string;
  gatewayRefundId?: string;
  createdAt: string;
  processedAt?: string;
}

export interface PaymentSummaryDto {
  id: string;
  orderId: string;
  amount: number;
  currency: string;
  status: string;
  createdAt: string;
}

export interface CreatePaymentRequest {
  orderId: string;
  customerId: string;
  amount: number;
  currency: string;
  paymentMethod: 'Card' | 'BankTransfer' | 'Wallet' | 'COD' | 'PayPal' | 'Stripe';
  paymentMethodDetails?: string;
}

export interface CreatePaymentResult {
  success: boolean;
  paymentId?: string;
  errorMessage?: string;
}

export interface AuthorizePaymentRequest {
  gatewayTransactionId: string;
  gatewayResponse?: string;
}

export interface PaymentOperationResult {
  success: boolean;
  errorMessage?: string;
}

export interface CapturePaymentRequest {
  captureTransactionId?: string;
}

export interface ProcessPaymentRequest {
  gatewayTransactionId: string;
  gatewayResponse?: string;
}

export interface RefundRequest {
  amount: number;
  reason: string;
  refundType?: 'CustomerRequest' | 'Damage' | 'WrongItem' | 'Other';
}

export interface RefundResult {
  success: boolean;
  refundId?: string;
  errorMessage?: string;
}

export interface ProcessRefundRequest {
  gatewayRefundId: string;
}

export interface VoidPaymentRequest {
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/payments`;

  // GET /api/payments/{id} - Get payment by ID
  getPayment(id: string): Observable<PaymentDetailsDto> {
    return this.http.get<PaymentDetailsDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/payments/order/{orderId} - Get payment by order
  getPaymentByOrder(orderId: string): Observable<PaymentDetailsDto> {
    return this.http.get<PaymentDetailsDto>(`${this.baseUrl}/order/${orderId}`);
  }

  // POST /api/payments - Create payment
  createPayment(request: CreatePaymentRequest): Observable<CreatePaymentResult> {
    return this.http.post<CreatePaymentResult>(this.baseUrl, request);
  }

  // POST /api/payments/{id}/authorize - Authorize payment
  authorizePayment(id: string, request: AuthorizePaymentRequest): Observable<PaymentOperationResult> {
    return this.http.post<PaymentOperationResult>(`${this.baseUrl}/${id}/authorize`, request);
  }

  // POST /api/payments/{id}/capture - Capture payment
  capturePayment(id: string, request?: CapturePaymentRequest): Observable<PaymentOperationResult> {
    return this.http.post<PaymentOperationResult>(`${this.baseUrl}/${id}/capture`, request || {});
  }

  // POST /api/payments/{id}/process - Process full payment
  processPayment(id: string, request: ProcessPaymentRequest): Observable<PaymentOperationResult> {
    return this.http.post<PaymentOperationResult>(`${this.baseUrl}/${id}/process`, request);
  }

  // POST /api/payments/{id}/refund - Request refund
  requestRefund(id: string, request: RefundRequest): Observable<RefundResult> {
    return this.http.post<RefundResult>(`${this.baseUrl}/${id}/refund`, request);
  }

  // POST /api/payments/{paymentId}/refund/{refundId}/process - Process refund
  processRefund(paymentId: string, refundId: string, request: ProcessRefundRequest): Observable<RefundResult> {
    return this.http.post<RefundResult>(`${this.baseUrl}/${paymentId}/refund/${refundId}/process`, request);
  }

  // POST /api/payments/{id}/void - Void payment
  voidPayment(id: string, request: VoidPaymentRequest): Observable<PaymentOperationResult> {
    return this.http.post<PaymentOperationResult>(`${this.baseUrl}/${id}/void`, request);
  }

  // GET /api/payments/customer/{customerId} - Get customer payments
  getCustomerPayments(customerId: string): Observable<PaymentSummaryDto[]> {
    return this.http.get<PaymentSummaryDto[]>(`${this.baseUrl}/customer/${customerId}`);
  }
}
