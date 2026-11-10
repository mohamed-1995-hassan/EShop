import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface ReviewDto {
  id: string;
  productId: string;
  customerId: string;
  customerName: string;
  rating: number;
  title?: string;
  content: string;
  images: string[];
  isVerifiedPurchase: boolean;
  status: ReviewStatus;
  helpfulVotes: number;
  unhelpfulVotes: number;
  response?: ReviewResponseDto;
  flagReason?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ReviewResponseDto {
  content: string;
  respondedBy: string;
  respondedAt: string;
}

export interface ProductReviewsDto {
  productId: string;
  reviews: ReviewDto[];
  totalCount: number;
  averageRating: number;
  ratingDistribution: { [rating: number]: number };
}

export interface ReviewSummaryDto {
  productId: string;
  averageRating: number;
  totalReviews: number;
  ratingDistribution: { [rating: number]: number };
  recommendationPercentage: number;
}

export type ReviewStatus = 'Pending' | 'Approved' | 'Rejected' | 'Flagged';

export interface CreateReviewRequest {
  productId: string;
  customerId: string;
  customerName: string;
  rating: number;
  title?: string;
  content: string;
  isVerifiedPurchase?: boolean;
}

export interface CreateReviewResult {
  success: boolean;
  reviewId?: string;
  errorMessage?: string;
}

export interface UpdateReviewRequest {
  rating: number;
  title?: string;
  content: string;
}

export interface AddReviewImageRequest {
  imageUrl: string;
  caption?: string;
}

export interface VoteReviewRequest {
  customerId: string;
  isHelpful: boolean;
}

export interface AddReviewResponseRequest {
  content: string;
  respondedBy: string;
}

export interface RejectReviewRequest {
  reason: string;
}

export interface FlagReviewRequest {
  customerId: string;
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class ReviewsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/reviews`;

  // GET /api/reviews/{id}
  getReview(id: string): Observable<ReviewDto> {
    return this.http.get<ReviewDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/reviews/product/{productId}
  getProductReviews(productId: string, skip = 0, take = 10, sortBy = 'newest'): Observable<ProductReviewsDto> {
    const params = new HttpParams()
      .set('skip', skip.toString())
      .set('take', take.toString())
      .set('sortBy', sortBy);
    return this.http.get<ProductReviewsDto>(`${this.baseUrl}/product/${productId}`, { params });
  }

  // POST /api/reviews - Create review
  createReview(request: CreateReviewRequest): Observable<CreateReviewResult> {
    return this.http.post<CreateReviewResult>(this.baseUrl, request);
  }

  // PUT /api/reviews/{id} - Update review
  updateReview(id: string, request: UpdateReviewRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  // POST /api/reviews/{id}/images - Add image
  addImage(reviewId: string, request: AddReviewImageRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${reviewId}/images`, request);
  }

  // POST /api/reviews/{id}/vote - Vote on review
  vote(reviewId: string, request: VoteReviewRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${reviewId}/vote`, request);
  }

  // POST /api/reviews/{id}/response - Add store response
  addResponse(reviewId: string, request: AddReviewResponseRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${reviewId}/response`, request);
  }

  // POST /api/reviews/{id}/approve - Approve review
  approve(reviewId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${reviewId}/approve`, {});
  }

  // POST /api/reviews/{id}/reject - Reject review
  reject(reviewId: string, request: RejectReviewRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${reviewId}/reject`, request);
  }

  // POST /api/reviews/{id}/flag - Flag review
  flag(reviewId: string, request: FlagReviewRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${reviewId}/flag`, request);
  }

  // GET /api/reviews/customer/{customerId}
  getCustomerReviews(customerId: string): Observable<ReviewDto[]> {
    return this.http.get<ReviewDto[]>(`${this.baseUrl}/customer/${customerId}`);
  }

  // GET /api/reviews/pending-moderation
  getPendingModeration(): Observable<ReviewDto[]> {
    return this.http.get<ReviewDto[]>(`${this.baseUrl}/pending-moderation`);
  }

  // GET /api/reviews/product/{productId}/summary
  getProductReviewSummary(productId: string): Observable<ReviewSummaryDto> {
    return this.http.get<ReviewSummaryDto>(`${this.baseUrl}/product/${productId}/summary`);
  }
}
