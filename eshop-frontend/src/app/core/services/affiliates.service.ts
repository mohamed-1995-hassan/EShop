import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface AffiliateDto {
  id: string;
  userId: string;
  name: string;
  email: string;
  website?: string;
  code: string;
  status: AffiliateStatus;
  commissionRate: number;
  totalEarnings: number;
  pendingEarnings: number;
  paidEarnings: number;
  currency: string;
  referrals: ReferralDto[];
  commissions: CommissionDto[];
  payouts: PayoutDto[];
  createdAt: string;
  approvedAt?: string;
}

export interface AffiliateSummaryDto {
  id: string;
  name: string;
  code: string;
  status: AffiliateStatus;
  totalEarnings: number;
  pendingEarnings: number;
  referralCount: number;
}

export interface ReferralDto {
  id: string;
  ipAddress: string;
  userAgent?: string;
  landingPage?: string;
  orderId?: string;
  customerId?: string;
  status: string;
  createdAt: string;
  convertedAt?: string;
}

export interface CommissionDto {
  id: string;
  orderId: string;
  orderAmount: number;
  commissionAmount: number;
  currency: string;
  description: string;
  status: string;
  createdAt: string;
}

export interface PayoutDto {
  id: string;
  amount: number;
  currency: string;
  method: PayoutMethod;
  transactionReference?: string;
  status: string;
  requestedAt: string;
  processedAt?: string;
}

export type AffiliateStatus = 'Pending' | 'Active' | 'Suspended' | 'Rejected';
export type PayoutMethod = 'BankTransfer' | 'PayPal' | 'Check' | 'StoreCredit';

export interface ApplyRequest {
  userId: string;
  name: string;
  email: string;
  website?: string;
}

export interface RejectRequest {
  reason: string;
}

export interface SuspendRequest {
  reason: string;
}

export interface TrackReferralRequest {
  ipAddress: string;
  userAgent?: string;
  landingPage?: string;
}

export interface ConvertReferralRequest {
  orderId: string;
  customerId: string;
}

export interface EarnCommissionRequest {
  orderId: string;
  orderAmount: number;
  currency: string;
  description: string;
}

export interface PayoutRequest {
  method: PayoutMethod;
}

export interface ProcessPayoutRequest {
  transactionReference: string;
}

export interface SetCommissionRateRequest {
  rate: number;
}

@Injectable({ providedIn: 'root' })
export class AffiliatesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/affiliates`;

  // GET /api/affiliates/{id}
  getAffiliate(id: string): Observable<AffiliateDto> {
    return this.http.get<AffiliateDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/affiliates/code/{code}
  getAffiliateByCode(code: string): Observable<AffiliateDto> {
    return this.http.get<AffiliateDto>(`${this.baseUrl}/code/${code}`);
  }

  // GET /api/affiliates/user/{userId}
  getAffiliateByUser(userId: string): Observable<AffiliateDto> {
    return this.http.get<AffiliateDto>(`${this.baseUrl}/user/${userId}`);
  }

  // GET /api/affiliates/pending
  getPendingAffiliates(): Observable<AffiliateSummaryDto[]> {
    return this.http.get<AffiliateSummaryDto[]>(`${this.baseUrl}/pending`);
  }

  // GET /api/affiliates/top-performers
  getTopPerformers(count = 10): Observable<AffiliateSummaryDto[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<AffiliateSummaryDto[]>(`${this.baseUrl}/top-performers`, { params });
  }

  // GET /api/affiliates/pending-payouts
  getPendingPayouts(): Observable<AffiliateSummaryDto[]> {
    return this.http.get<AffiliateSummaryDto[]>(`${this.baseUrl}/pending-payouts`);
  }

  // POST /api/affiliates/apply - Apply for affiliate
  apply(request: ApplyRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/apply`, request);
  }

  // POST /api/affiliates/{id}/approve
  approve(affiliateId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${affiliateId}/approve`, {});
  }

  // POST /api/affiliates/{id}/reject
  reject(affiliateId: string, request: RejectRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${affiliateId}/reject`, request);
  }

  // POST /api/affiliates/{id}/suspend
  suspend(affiliateId: string, request: SuspendRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${affiliateId}/suspend`, request);
  }

  // POST /api/affiliates/{id}/referrals - Track referral
  trackReferral(affiliateId: string, request: TrackReferralRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${affiliateId}/referrals`, request);
  }

  // POST /api/affiliates/{id}/referrals/{referralId}/convert
  convertReferral(affiliateId: string, referralId: string, request: ConvertReferralRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${affiliateId}/referrals/${referralId}/convert`, request);
  }

  // POST /api/affiliates/{id}/commissions - Earn commission
  earnCommission(affiliateId: string, request: EarnCommissionRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${affiliateId}/commissions`, request);
  }

  // POST /api/affiliates/{id}/payouts - Request payout
  requestPayout(affiliateId: string, request: PayoutRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${affiliateId}/payouts`, request);
  }

  // POST /api/affiliates/{id}/payouts/{payoutId}/process
  processPayout(affiliateId: string, payoutId: string, request: ProcessPayoutRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${affiliateId}/payouts/${payoutId}/process`, request);
  }

  // PUT /api/affiliates/{id}/commission-rate
  setCommissionRate(affiliateId: string, request: SetCommissionRateRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${affiliateId}/commission-rate`, request);
  }
}
