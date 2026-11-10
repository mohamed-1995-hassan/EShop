import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { CartService } from '@core/services/cart.service';
import { OrdersService, CreateOrderRequest } from '@core/services/orders.service';
import { AuthService } from '@core/auth/auth.service';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule, HeaderComponent, FooterComponent],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css'
})
export class CheckoutComponent implements OnInit {
  cartService = inject(CartService);
  private ordersService = inject(OrdersService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  
  addressForm = this.fb.group({
    street: ['', Validators.required],
    city: ['', Validators.required],
    postalCode: ['', Validators.required],
    country: ['', Validators.required]
  });
  
  paymentMethod = signal<'card' | 'paypal'>('card');
  processing = signal(false);
  error = signal<string | null>(null);
  
  ngOnInit() {
    if (!this.authService.loggedIn()) {
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: '/checkout' } });
    }
  }
  
  placeOrder() {
    if (!this.addressForm.valid || this.processing()) return;
    
    const user = this.authService.currentUser();
    if (!user) return;
    
    this.processing.set(true);
    this.error.set(null);
    
    const request: CreateOrderRequest = {
      customerId: user.id,
      shippingAddress: {
        street: this.addressForm.value.street!,
        city: this.addressForm.value.city!,
        postalCode: this.addressForm.value.postalCode!,
        country: this.addressForm.value.country!
      },
      items: this.cartService.items().map(item => ({
        productId: item.productId,
        productName: item.productName,
        unitPrice: item.unitPrice,
        quantity: item.quantity
      }))
    };
    
    this.ordersService.createOrder(request).subscribe({
      next: (result) => {
        if (result.success && result.orderId) {
          this.cartService.clearCart().subscribe();
          this.router.navigate(['/orders', result.orderId]);
        } else {
          this.error.set(result.errorMessage || 'Failed to create order');
        }
        this.processing.set(false);
      },
      error: (err) => {
        console.error('Error creating order:', err);
        this.error.set('Failed to create order. Please try again.');
        this.processing.set(false);
      }
    });
  }
}
