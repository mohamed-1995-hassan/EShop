import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { CartService } from '@core/services/cart.service';
import { AuthService } from '@core/auth/auth.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.css'
})
export class CartComponent {
  cartService = inject(CartService);
  private auth = inject(AuthService);
  private router = inject(Router);
  
  couponCode = '';
  couponError = signal<string | null>(null);
  processing = signal(false);
  
  updateQuantity(itemId: string, quantity: number) {
    if (quantity < 1 || this.processing()) return;
    this.processing.set(true);
    
    this.cartService.updateItemQuantity(itemId, quantity).subscribe({
      next: () => this.processing.set(false),
      error: (err) => {
        console.error('Error updating quantity:', err);
        this.processing.set(false);
      }
    });
  }
  
  removeItem(itemId: string) {
    if (this.processing()) return;
    this.processing.set(true);
    
    this.cartService.removeItem(itemId).subscribe({
      next: () => this.processing.set(false),
      error: (err) => {
        console.error('Error removing item:', err);
        this.processing.set(false);
      }
    });
  }
  
  clearCart() {
    if (confirm('Are you sure you want to clear your cart?')) {
      this.processing.set(true);
      this.cartService.clearCart().subscribe({
        next: () => this.processing.set(false),
        error: (err) => {
          console.error('Error clearing cart:', err);
          this.processing.set(false);
        }
      });
    }
  }
  
  applyCoupon() {
    if (!this.couponCode.trim() || this.processing()) return;
    
    this.processing.set(true);
    this.couponError.set(null);
    this.cartService.applyCoupon(this.couponCode).subscribe({
      next: () => {
        this.couponCode = '';
        this.processing.set(false);
      },
      error: (err) => {
        this.couponError.set(err.error?.message || 'Invalid coupon code');
        this.processing.set(false);
      }
    });
  }
  
  removeCoupon() {
    if (this.processing()) return;
    this.processing.set(true);
    
    this.cartService.removeCoupon().subscribe({
      next: () => this.processing.set(false),
      error: (err) => {
        console.error('Error removing coupon:', err);
        this.processing.set(false);
      }
    });
  }
  
  checkout() {
    if (!this.auth.loggedIn()) {
      this.router.navigate(['/auth/login'], {
        queryParams: { returnUrl: '/checkout' }
      });
      return;
    }
    this.router.navigate(['/checkout']);
  }
}
