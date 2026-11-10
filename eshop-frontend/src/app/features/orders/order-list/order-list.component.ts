import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { OrdersService, OrderSummaryDto } from '@core/services/orders.service';
import { AuthService } from '@core/auth/auth.service';
import { Order } from '@shared/models';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent, LoadingComponent],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.css'
})
export class OrderListComponent implements OnInit {
  private ordersService = inject(OrdersService);
  private authService = inject(AuthService);
  
  orders = signal<OrderSummaryDto[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  
  currentPage = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  totalPages = signal(0);
  
  ngOnInit() {
    this.loadOrders();
  }
  
  loadOrders() {
    const customerId = this.authService.currentUser()?.id;
    if (!customerId) {
      this.loading.set(false);
      return;
    }
    
    this.loading.set(true);
    this.ordersService.getCustomerOrders(customerId, this.currentPage(), this.pageSize()).pipe(
      catchError(err => {
        console.error('Error loading orders:', err);
        this.error.set('Failed to load orders');
        return of({ items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 });
      })
    ).subscribe(result => {
      this.orders.set(result.items);
      this.totalCount.set(result.totalCount);
      this.totalPages.set(result.totalPages);
      this.loading.set(false);
    });
  }
  
  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadOrders();
    }
  }
}
