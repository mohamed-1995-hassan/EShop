import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '@core/auth/auth.service';
import { CartService } from '@core/services/cart.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent {
  auth = inject(AuthService);
  private cartService = inject(CartService);
  
  searchQuery = '';
  showMobileSearch = signal(false);
  showMobileMenu = signal(false);
  showUserMenu = signal(false);
  
  cartItemCount = computed(() => this.cartService.itemCount());
  wishlistCount = signal(0);
  
  search() {
    if (this.searchQuery.trim()) {
      window.location.href = `/products/search?q=${encodeURIComponent(this.searchQuery)}`;
    }
  }
  
  toggleMobileSearch() {
    this.showMobileSearch.update(v => !v);
  }
  
  toggleMobileMenu() {
    this.showMobileMenu.update(v => !v);
  }
  
  closeMobileMenu() {
    this.showMobileMenu.set(false);
  }
  
  toggleUserMenu() {
    this.showUserMenu.update(v => !v);
  }
  
  logout() {
    this.auth.logout();
    this.showUserMenu.set(false);
  }
}
