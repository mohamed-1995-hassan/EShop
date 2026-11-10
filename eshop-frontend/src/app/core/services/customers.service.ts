import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface CustomerProfileDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone?: string;
  dateOfBirth?: string;
  addresses: CustomerAddressDto[];
  loyaltyPoints: number;
  loyaltyTier: string;
  totalOrders: number;
  totalSpent: number;
  createdAt: string;
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

export interface RegisterCustomerRequest {
  email: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  phone?: string;
  referralCode?: string;
}

export interface CreateCustomerResult {
  success: boolean;
  customerId?: string;
  errorMessage?: string;
}

export interface AddAddressRequest {
  label: string;
  street: string;
  city: string;
  country: string;
  postalCode: string;
  isDefault?: boolean;
}

export interface AddAddressResult {
  success: boolean;
  addressId?: string;
  errorMessage?: string;
}

export interface LoyaltySummaryDto {
  customerId: string;
  points: number;
  tier: string;
  lifetimePoints: number;
  pointsToNextTier: number;
  recentTransactions: LoyaltyTransactionDto[];
}

export interface LoyaltyTransactionDto {
  id: string;
  type: string;
  points: number;
  description: string;
  createdAt: string;
}

export interface RedeemPointsRequest {
  points: number;
  description: string;
  orderId?: string;
}

export interface RedeemPointsResult {
  success: boolean;
  newBalance?: number;
  errorMessage?: string;
}

export interface WalletTransactionsResult {
  balance: number;
  currency: string;
  transactions: WalletTransactionDto[];
  totalCount: number;
}

export interface WalletTransactionDto {
  id: string;
  type: string;
  amount: number;
  description: string;
  createdAt: string;
}

export interface CreditWalletRequest {
  amount: number;
  description: string;
  type: 'Credit' | 'Refund' | 'Bonus' | 'Adjustment';
}

export interface WalletOperationResult {
  success: boolean;
  newBalance?: number;
  errorMessage?: string;
}

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/customers`;

  // GET /api/customers/{id} - Get customer profile
  getProfile(id: string): Observable<CustomerProfileDto> {
    return this.http.get<CustomerProfileDto>(`${this.baseUrl}/${id}`);
  }

  // POST /api/customers - Register new customer
  register(request: RegisterCustomerRequest): Observable<CreateCustomerResult> {
    return this.http.post<CreateCustomerResult>(this.baseUrl, request);
  }

  // POST /api/customers/{id}/addresses - Add address
  addAddress(customerId: string, request: AddAddressRequest): Observable<AddAddressResult> {
    return this.http.post<AddAddressResult>(`${this.baseUrl}/${customerId}/addresses`, request);
  }

  // GET /api/customers/{id}/loyalty - Get loyalty summary
  getLoyaltySummary(customerId: string): Observable<LoyaltySummaryDto> {
    return this.http.get<LoyaltySummaryDto>(`${this.baseUrl}/${customerId}/loyalty`);
  }

  // POST /api/customers/{id}/loyalty/redeem - Redeem points
  redeemPoints(customerId: string, request: RedeemPointsRequest): Observable<RedeemPointsResult> {
    return this.http.post<RedeemPointsResult>(`${this.baseUrl}/${customerId}/loyalty/redeem`, request);
  }

  // GET /api/customers/{id}/wallet - Get wallet transactions
  getWalletTransactions(customerId: string, page = 1, pageSize = 20): Observable<WalletTransactionsResult> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<WalletTransactionsResult>(`${this.baseUrl}/${customerId}/wallet`, { params });
  }

  // POST /api/customers/{id}/wallet/credit - Credit wallet
  creditWallet(customerId: string, request: CreditWalletRequest): Observable<WalletOperationResult> {
    return this.http.post<WalletOperationResult>(`${this.baseUrl}/${customerId}/wallet/credit`, request);
  }
}
