import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface AuctionDto {
  id: string;
  productId: string;
  sellerId: string;
  title: string;
  description: string;
  imageUrl?: string;
  type: AuctionType;
  status: AuctionStatus;
  startingPrice: number;
  currentPrice: number;
  reservePrice?: number;
  buyNowPrice?: number;
  currency: string;
  bids: BidDto[];
  watchers: string[];
  winnerId?: string;
  startTime: string;
  endTime: string;
  createdAt: string;
  endedAt?: string;
}

export interface AuctionSummaryDto {
  id: string;
  title: string;
  imageUrl?: string;
  currentPrice: number;
  currency: string;
  bidCount: number;
  watcherCount: number;
  status: AuctionStatus;
  endTime: string;
}

export interface BidDto {
  id: string;
  bidderId: string;
  amount: number;
  currency: string;
  maxAutoBid?: number;
  isWinning: boolean;
  createdAt: string;
}

export type AuctionType = 'Standard' | 'Reserve' | 'BuyNow' | 'Dutch';
export type AuctionStatus = 'Draft' | 'Active' | 'Ended' | 'Sold' | 'Cancelled' | 'NoSale';
export type AuctionEndReason = 'TimeExpired' | 'BuyNowPurchased' | 'SellerEnded' | 'Cancelled';

export interface CreateAuctionRequest {
  productId: string;
  sellerId: string;
  title: string;
  description: string;
  startingPrice: number;
  currency: string;
  startTime: string;
  endTime: string;
  reservePrice?: number;
  buyNowPrice?: number;
  type?: AuctionType;
  imageUrl?: string;
}

export interface PlaceBidRequest {
  bidderId: string;
  bidAmount: number;
  currency: string;
  maxAutoBid?: number;
}

export interface PlaceProxyBidRequest {
  bidderId: string;
  maxBidAmount: number;
  currency: string;
}

export interface BuyNowRequest {
  buyerId: string;
}

export interface EndAuctionRequest {
  reason?: AuctionEndReason;
}

export interface CancelAuctionRequest {
  reason: string;
}

export interface AddWatcherRequest {
  customerId: string;
}

export interface MarkPaidRequest {
  paymentId: string;
}

@Injectable({ providedIn: 'root' })
export class AuctionsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/auctions`;

  // GET /api/auctions/{id}
  getAuction(id: string): Observable<AuctionDto> {
    return this.http.get<AuctionDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/auctions/active
  getActiveAuctions(): Observable<AuctionSummaryDto[]> {
    return this.http.get<AuctionSummaryDto[]>(`${this.baseUrl}/active`);
  }

  // GET /api/auctions/ending-soon
  getEndingSoon(withinMinutes = 60): Observable<AuctionSummaryDto[]> {
    const params = new HttpParams().set('withinMinutes', withinMinutes.toString());
    return this.http.get<AuctionSummaryDto[]>(`${this.baseUrl}/ending-soon`, { params });
  }

  // GET /api/auctions/seller/{sellerId}
  getSellerAuctions(sellerId: string): Observable<AuctionSummaryDto[]> {
    return this.http.get<AuctionSummaryDto[]>(`${this.baseUrl}/seller/${sellerId}`);
  }

  // GET /api/auctions/bidder/{bidderId}
  getBidderAuctions(bidderId: string): Observable<AuctionSummaryDto[]> {
    return this.http.get<AuctionSummaryDto[]>(`${this.baseUrl}/bidder/${bidderId}`);
  }

  // GET /api/auctions/won/{bidderId}
  getWonAuctions(bidderId: string): Observable<AuctionSummaryDto[]> {
    return this.http.get<AuctionSummaryDto[]>(`${this.baseUrl}/won/${bidderId}`);
  }

  // POST /api/auctions - Create auction
  createAuction(request: CreateAuctionRequest): Observable<string> {
    return this.http.post<string>(this.baseUrl, request);
  }

  // POST /api/auctions/{auctionId}/bid - Place bid
  placeBid(auctionId: string, request: PlaceBidRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${auctionId}/bid`, request);
  }

  // POST /api/auctions/{auctionId}/proxy-bid - Place proxy bid
  placeProxyBid(auctionId: string, request: PlaceProxyBidRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${auctionId}/proxy-bid`, request);
  }

  // POST /api/auctions/{auctionId}/buy-now - Buy now
  buyNow(auctionId: string, request: BuyNowRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${auctionId}/buy-now`, request);
  }

  // POST /api/auctions/{auctionId}/end - End auction
  endAuction(auctionId: string, request?: EndAuctionRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${auctionId}/end`, request || {});
  }

  // POST /api/auctions/{auctionId}/cancel - Cancel auction
  cancelAuction(auctionId: string, request: CancelAuctionRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${auctionId}/cancel`, request);
  }

  // POST /api/auctions/{auctionId}/watch - Add watcher
  addWatcher(auctionId: string, request: AddWatcherRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${auctionId}/watch`, request);
  }

  // POST /api/auctions/{auctionId}/paid - Mark as paid
  markPaid(auctionId: string, request: MarkPaidRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${auctionId}/paid`, request);
  }
}
