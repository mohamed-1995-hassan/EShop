import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { WishlistsService, WishlistDto, WishlistItemDto } from '@core/services/wishlists.service';
import { CartService } from '@core/services/cart.service';
import { AuthService } from '@core/auth/auth.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-wishlists',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent, LoadingComponent],
  templateUrl: './wishlists.component.html',
  styleUrl: './wishlists.component.css'
})
export class WishlistsComponent implements OnInit {
  private wishlistsService = inject(WishlistsService);
  private cartService = inject(CartService);
  private authService = inject(AuthService);
  
  wishlists = signal<WishlistDto[]>([]);
  items = signal<WishlistItemDto[]>([]);
  loading = signal(true);
  
  ngOnInit() {
    this.loadWishlists();
  }
  
  loadWishlists() {
    const customerId = this.authService.currentUser()?.id;
    if (!customerId) {
      this.loading.set(false);
      return;
    }
    
    this.loading.set(true);
    this.wishlistsService.getCustomerWishlists(customerId).pipe(
      catchError(err => {
        console.error('Error loading wishlists:', err);
        return of([]);
      })
    ).subscribe(wishlists => {
      this.wishlists.set(wishlists);
      // Flatten all items from all wishlists
      const allItems = wishlists.flatMap(w => w.items);
      this.items.set(allItems);
      this.loading.set(false);
    });
  }
  
  addToCart(item: WishlistItemDto) {
    this.cartService.quickAdd({
      id: item.productId,
      name: item.productName,
      price: item.price,
      image: item.imageUrl,
      sku: item.productId
    });
  }
  
  removeFromWishlist(item: WishlistItemDto) {
    const wishlist = this.wishlists().find(w => w.items.some(i => i.id === item.id));
    if (!wishlist) return;
    
    this.wishlistsService.removeItem(wishlist.id, item.id).subscribe({
      next: () => {
        this.items.update(items => items.filter(i => i.id !== item.id));
      },
      error: (err) => {
        console.error('Error removing item:', err);
      }
    });
  }
}
