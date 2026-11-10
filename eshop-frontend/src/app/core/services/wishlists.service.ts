import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface WishlistDto {
  id: string;
  customerId: string;
  name: string;
  type: WishlistType;
  privacy: WishlistPrivacy;
  description?: string;
  shareToken?: string;
  items: WishlistItemDto[];
  createdAt: string;
  updatedAt: string;
}

export interface WishlistItemDto {
  id: string;
  productId: string;
  productName: string;
  price: number;
  currency: string;
  imageUrl?: string;
  desiredQuantity: number;
  notes?: string;
  isPurchased: boolean;
  purchasedBy?: string;
  addedAt: string;
}

export type WishlistType = 'Standard' | 'Birthday' | 'Wedding' | 'Baby' | 'Holiday' | 'Custom';
export type WishlistPrivacy = 'Private' | 'Shared' | 'Public';

export interface CreateWishlistRequest {
  customerId: string;
  name: string;
  type: WishlistType;
  privacy?: WishlistPrivacy;
  description?: string;
}

export interface CreateDefaultWishlistRequest {
  customerId: string;
}

export interface AddWishlistItemRequest {
  productId: string;
  productName: string;
  price: number;
  currency: string;
  imageUrl?: string;
  desiredQuantity?: number;
  notes?: string;
}

export interface MarkPurchasedRequest {
  purchasedBy?: string;
}

export interface UpdatePrivacyRequest {
  privacy: WishlistPrivacy;
}

@Injectable({ providedIn: 'root' })
export class WishlistsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/wishlists`;

  // GET /api/wishlists/{id}
  getWishlist(id: string): Observable<WishlistDto> {
    return this.http.get<WishlistDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/wishlists/customer/{customerId}
  getCustomerWishlists(customerId: string): Observable<WishlistDto[]> {
    return this.http.get<WishlistDto[]>(`${this.baseUrl}/customer/${customerId}`);
  }

  // GET /api/wishlists/shared/{shareToken}
  getSharedWishlist(shareToken: string): Observable<WishlistDto> {
    return this.http.get<WishlistDto>(`${this.baseUrl}/shared/${shareToken}`);
  }

  // GET /api/wishlists/customer/{customerId}/product/{productId}/exists
  isProductInWishlist(customerId: string, productId: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.baseUrl}/customer/${customerId}/product/${productId}/exists`);
  }

  // POST /api/wishlists - Create wishlist
  createWishlist(request: CreateWishlistRequest): Observable<string> {
    return this.http.post<string>(this.baseUrl, request);
  }

  // POST /api/wishlists/default - Create default wishlist
  createDefaultWishlist(request: CreateDefaultWishlistRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/default`, request);
  }

  // POST /api/wishlists/{wishlistId}/items - Add item
  addItem(wishlistId: string, request: AddWishlistItemRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${wishlistId}/items`, request);
  }

  // DELETE /api/wishlists/{wishlistId}/items/{itemId} - Remove item
  removeItem(wishlistId: string, itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${wishlistId}/items/${itemId}`);
  }

  // POST /api/wishlists/{wishlistId}/items/{itemId}/purchased - Mark as purchased
  markItemPurchased(wishlistId: string, itemId: string, request: MarkPurchasedRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${wishlistId}/items/${itemId}/purchased`, request);
  }

  // PUT /api/wishlists/{wishlistId}/privacy - Update privacy
  updatePrivacy(wishlistId: string, request: UpdatePrivacyRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${wishlistId}/privacy`, request);
  }
}
