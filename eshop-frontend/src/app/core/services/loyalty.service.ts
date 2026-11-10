import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface LoyaltyAccountDto {
  id: string;
  customerId: string;
  points: number;
  tier: LoyaltyTier;
  lifetimePoints: number;
  lifetimeValue: number;
  pointsToNextTier: number;
  transactions: PointTransactionDto[];
  redemptions: PointRedemptionDto[];
  status: string;
  enrolledAt: string;
}

export interface LoyaltySummaryDto {
  id: string;
  customerId: string;
  points: number;
  tier: LoyaltyTier;
  lifetimeValue: number;
  isAtRisk: boolean;
}

export interface PointTransactionDto {
  id: string;
  type: PointTransactionType;
  points: number;
  orderId?: string;
  reason: string;
  createdAt: string;
  expiresAt?: string;
}

export interface PointRedemptionDto {
  id: string;
  points: number;
  type: string;
  discountAmount?: number;
  rewardName?: string;
  orderId?: string;
  createdAt: string;
}

export interface LoyaltyProgramStatsDto {
  totalMembers: number;
  activeMembers: number;
  totalPointsOutstanding: number;
  totalPointsRedeemed: number;
  averagePointsPerMember: number;
  membersByTier: { [tier: string]: number };
}

export type LoyaltyTier = 'Bronze' | 'Silver' | 'Gold' | 'Platinum' | 'Diamond';
export type PointTransactionType = 'Purchase' | 'Bonus' | 'Referral' | 'Birthday' | 'Review' | 'Adjustment' | 'Expiry';

export interface EnrollRequest {
  customerId: string;
  welcomeBonus?: number;
}

export interface EarnPointsRequest {
  orderId: string;
  orderAmount: number;
  currency: string;
  pointsPerCurrency?: number;
}

export interface EarnBonusRequest {
  points: number;
  reason: string;
  type?: PointTransactionType;
}

export interface RedeemDiscountRequest {
  points: number;
  discountAmount: number;
  currency: string;
  orderId: string;
}

export interface RedeemRewardRequest {
  rewardId: string;
  rewardName: string;
  requiredPoints: number;
}

export interface SuspendRequest {
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class LoyaltyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/loyalty`;

  // GET /api/loyalty/{id}
  getAccount(id: string): Observable<LoyaltyAccountDto> {
    return this.http.get<LoyaltyAccountDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/loyalty/customer/{customerId}
  getByCustomer(customerId: string): Observable<LoyaltyAccountDto> {
    return this.http.get<LoyaltyAccountDto>(`${this.baseUrl}/customer/${customerId}`);
  }

  // GET /api/loyalty/tier/{tier}
  getByTier(tier: LoyaltyTier): Observable<LoyaltySummaryDto[]> {
    return this.http.get<LoyaltySummaryDto[]>(`${this.baseUrl}/tier/${tier}`);
  }

  // GET /api/loyalty/top/{count}
  getTopMembers(count = 10): Observable<LoyaltySummaryDto[]> {
    return this.http.get<LoyaltySummaryDto[]>(`${this.baseUrl}/top/${count}`);
  }

  // GET /api/loyalty/expiring/{days}
  getWithExpiringPoints(days = 30): Observable<LoyaltySummaryDto[]> {
    return this.http.get<LoyaltySummaryDto[]>(`${this.baseUrl}/expiring/${days}`);
  }

  // GET /api/loyalty/stats
  getStats(): Observable<LoyaltyProgramStatsDto> {
    return this.http.get<LoyaltyProgramStatsDto>(`${this.baseUrl}/stats`);
  }

  // POST /api/loyalty/enroll
  enroll(request: EnrollRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/enroll`, request);
  }

  // POST /api/loyalty/{accountId}/earn
  earnPoints(accountId: string, request: EarnPointsRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${accountId}/earn`, request);
  }

  // POST /api/loyalty/{accountId}/bonus
  earnBonusPoints(accountId: string, request: EarnBonusRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${accountId}/bonus`, request);
  }

  // POST /api/loyalty/{accountId}/redeem/discount
  redeemForDiscount(accountId: string, request: RedeemDiscountRequest): Observable<PointRedemptionDto> {
    return this.http.post<PointRedemptionDto>(`${this.baseUrl}/${accountId}/redeem/discount`, request);
  }

  // POST /api/loyalty/{accountId}/redeem/reward
  redeemForReward(accountId: string, request: RedeemRewardRequest): Observable<PointRedemptionDto> {
    return this.http.post<PointRedemptionDto>(`${this.baseUrl}/${accountId}/redeem/reward`, request);
  }

  // POST /api/loyalty/{accountId}/suspend
  suspend(accountId: string, request: SuspendRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${accountId}/suspend`, request);
  }

  // POST /api/loyalty/{accountId}/reactivate
  reactivate(accountId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${accountId}/reactivate`, {});
  }
}
