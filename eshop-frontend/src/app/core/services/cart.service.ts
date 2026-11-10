import { Injectable, inject, signal, computed } from '@angular/core';
import { ApiService } from './api.service';
import { Cart, CartItem } from '@shared/models';
import { Observable, tap, catchError, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private api = inject(ApiService);
  
  private cart = signal<Cart | null>(null);
  private loading = signal<boolean>(false);
  
  // Computed values
  readonly currentCart = computed(() => this.cart());
  readonly items = computed(() => this.cart()?.items ?? []);
  readonly itemCount = computed(() => this.cart()?.itemCount ?? 0);
  readonly subtotal = computed(() => this.cart()?.subtotal ?? 0);
  readonly discount = computed(() => this.cart()?.discount ?? 0);
  readonly tax = computed(() => this.cart()?.tax ?? 0);
  readonly shipping = computed(() => this.cart()?.shipping ?? 0);
  readonly total = computed(() => this.cart()?.total ?? 0);
  readonly isLoading = computed(() => this.loading());
  readonly isEmpty = computed(() => this.itemCount() === 0);
  
  constructor() {
    this.loadCart();
  }
  
  // Load cart from server or local storage
  loadCart(): void {
    const localCart = localStorage.getItem('cart');
    if (localCart) {
      try {
        this.cart.set(JSON.parse(localCart));
      } catch {
        this.cart.set(this.createEmptyCart());
      }
    } else {
      this.cart.set(this.createEmptyCart());
    }
  }
  
  // Get cart from server
  fetchCart(): Observable<Cart> {
    this.loading.set(true);
    return this.api.get<Cart>('carts/my-cart').pipe(
      tap(cart => {
        this.cart.set(cart);
        this.saveCartToStorage();
        this.loading.set(false);
      }),
      catchError(err => {
        this.loading.set(false);
        return of(this.createEmptyCart());
      })
    );
  }
  
  // Add item to cart
  addItem(productId: string, quantity: number = 1, variantId?: string): Observable<Cart> {
    this.loading.set(true);
    return this.api.post<Cart>('carts/items', { productId, quantity, variantId }).pipe(
      tap(cart => {
        this.cart.set(cart);
        this.saveCartToStorage();
        this.loading.set(false);
      }),
      catchError(err => {
        this.loading.set(false);
        throw err;
      })
    );
  }
  
  // Update item quantity
  updateItemQuantity(itemId: string, quantity: number): Observable<Cart> {
    this.loading.set(true);
    return this.api.put<Cart>(`carts/items/${itemId}`, { quantity }).pipe(
      tap(cart => {
        this.cart.set(cart);
        this.saveCartToStorage();
        this.loading.set(false);
      }),
      catchError(err => {
        this.loading.set(false);
        throw err;
      })
    );
  }
  
  // Remove item from cart
  removeItem(itemId: string): Observable<Cart> {
    this.loading.set(true);
    return this.api.delete<Cart>(`carts/items/${itemId}`).pipe(
      tap(cart => {
        this.cart.set(cart);
        this.saveCartToStorage();
        this.loading.set(false);
      }),
      catchError(err => {
        this.loading.set(false);
        throw err;
      })
    );
  }
  
  // Clear cart
  clearCart(): Observable<void> {
    this.loading.set(true);
    return this.api.delete<void>('carts').pipe(
      tap(() => {
        this.cart.set(this.createEmptyCart());
        this.saveCartToStorage();
        this.loading.set(false);
      }),
      catchError(err => {
        this.loading.set(false);
        throw err;
      })
    );
  }
  
  // Apply coupon
  applyCoupon(code: string): Observable<Cart> {
    this.loading.set(true);
    return this.api.post<Cart>('carts/apply-coupon', { code }).pipe(
      tap(cart => {
        this.cart.set(cart);
        this.saveCartToStorage();
        this.loading.set(false);
      }),
      catchError(err => {
        this.loading.set(false);
        throw err;
      })
    );
  }
  
  // Remove coupon
  removeCoupon(): Observable<Cart> {
    this.loading.set(true);
    return this.api.delete<Cart>('carts/coupon').pipe(
      tap(cart => {
        this.cart.set(cart);
        this.saveCartToStorage();
        this.loading.set(false);
      }),
      catchError(err => {
        this.loading.set(false);
        throw err;
      })
    );
  }
  
  // Helper methods
  private createEmptyCart(): Cart {
    return {
      id: '',
      items: [],
      subtotal: 0,
      discount: 0,
      tax: 0,
      shipping: 0,
      total: 0,
      itemCount: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    };
  }
  
  private saveCartToStorage(): void {
    const cart = this.cart();
    if (cart) {
      localStorage.setItem('cart', JSON.stringify(cart));
    }
  }
  
  // Quick add for offline support
  quickAdd(product: { id: string; name: string; price: number; image?: string; sku: string }, quantity: number = 1): void {
    const cart = this.cart() ?? this.createEmptyCart();
    const existingItem = cart.items.find(i => i.productId === product.id);
    
    if (existingItem) {
      existingItem.quantity += quantity;
      existingItem.totalPrice = existingItem.unitPrice * existingItem.quantity;
    } else {
      const newItem: CartItem = {
        id: `temp-${Date.now()}`,
        productId: product.id,
        productName: product.name,
        productImage: product.image,
        sku: product.sku,
        quantity,
        unitPrice: product.price,
        totalPrice: product.price * quantity
      };
      cart.items.push(newItem);
    }
    
    this.recalculateCart(cart);
    this.cart.set(cart);
    this.saveCartToStorage();
  }
  
  private recalculateCart(cart: Cart): void {
    cart.itemCount = cart.items.reduce((sum, item) => sum + item.quantity, 0);
    cart.subtotal = cart.items.reduce((sum, item) => sum + item.totalPrice, 0);
    cart.total = cart.subtotal - cart.discount + cart.tax + cart.shipping;
    cart.updatedAt = new Date().toISOString();
  }
}
