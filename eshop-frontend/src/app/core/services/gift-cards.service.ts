import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface GiftCardDto {
  id: string;
  code: string;
  type: GiftCardType;
  initialBalance: number;
  currentBalance: number;
  currency: string;
  purchasedByCustomerId?: string;
  recipientEmail?: string;
  recipientName?: string;
  personalMessage?: string;
  scheduledDelivery?: string;
  status: string;
  expiresAt?: string;
  activatedAt?: string;
  createdAt: string;
  transactions: GiftCardTransactionDto[];
}

export interface GiftCardBalanceDto {
  code: string;
  balance: number;
  currency: string;
  status: string;
  expiresAt?: string;
}

export interface GiftCardTransactionDto {
  id: string;
  type: string;
  amount: number;
  orderId?: string;
  orderNumber?: string;
  createdAt: string;
}

export type GiftCardType = 'Digital' | 'Physical' | 'StoreCredit';

export interface CreateDigitalGiftCardRequest {
  amount: number;
  currency: string;
  purchasedByCustomerId: string;
  recipientEmail: string;
  recipientName: string;
  personalMessage?: string;
  scheduledDelivery?: string;
  expiryYears?: number;
}

export interface CreatePhysicalGiftCardRequest {
  amount: number;
  currency: string;
  purchasedByCustomerId?: string;
  expiryYears?: number;
}

export interface CreateStoreCreditRequest {
  amount: number;
  currency: string;
  customerId: string;
  reason: string;
  isReloadable?: boolean;
}

export interface RedeemGiftCardRequest {
  code: string;
  amount: number;
  currency: string;
  orderId: string;
  orderNumber: string;
}

export interface ReloadGiftCardRequest {
  amount: number;
  currency: string;
}

export interface SuspendGiftCardRequest {
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class GiftCardsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/giftcards`;

  // GET /api/giftcards/{id}
  getGiftCard(id: string): Observable<GiftCardDto> {
    return this.http.get<GiftCardDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/giftcards/balance/{code}
  checkBalance(code: string): Observable<GiftCardBalanceDto> {
    return this.http.get<GiftCardBalanceDto>(`${this.baseUrl}/balance/${code}`);
  }

  // GET /api/giftcards/customer/{customerId}
  getCustomerGiftCards(customerId: string): Observable<GiftCardDto[]> {
    return this.http.get<GiftCardDto[]>(`${this.baseUrl}/customer/${customerId}`);
  }

  // GET /api/giftcards/liability
  getTotalLiability(): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/liability`);
  }

  // POST /api/giftcards/digital
  createDigital(request: CreateDigitalGiftCardRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/digital`, request);
  }

  // POST /api/giftcards/physical
  createPhysical(request: CreatePhysicalGiftCardRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/physical`, request);
  }

  // POST /api/giftcards/store-credit
  createStoreCredit(request: CreateStoreCreditRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/store-credit`, request);
  }

  // POST /api/giftcards/{id}/activate
  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/activate`, {});
  }

  // POST /api/giftcards/redeem
  redeem(request: RedeemGiftCardRequest): Observable<number> {
    return this.http.post<number>(`${this.baseUrl}/redeem`, request);
  }

  // POST /api/giftcards/{id}/reload
  reload(id: string, request: ReloadGiftCardRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/reload`, request);
  }

  // POST /api/giftcards/{id}/suspend
  suspend(id: string, request: SuspendGiftCardRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/suspend`, request);
  }
}
