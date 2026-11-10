import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { OrdersService, OrderDetailsDto } from '@core/services/orders.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent, LoadingComponent],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.css'
})
export class OrderDetailComponent implements OnInit {
  private ordersService = inject(OrdersService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  
  order = signal<OrderDetailsDto | null>(null);
  loading = signal(true);
  cancelling = signal(false);
  
  ngOnInit() {
    const orderId = this.route.snapshot.paramMap.get('id');
    if (orderId) this.loadOrder(orderId);
  }
  
  loadOrder(id: string) {
    this.loading.set(true);
    this.ordersService.getOrder(id).pipe(
      catchError(err => {
        console.error('Error loading order:', err);
        return of(null);
      })
    ).subscribe(order => {
      this.order.set(order);
      this.loading.set(false);
    });
  }
  
  cancelOrder() {
    const order = this.order();
    if (!order || this.cancelling()) return;
    
    if (confirm('Are you sure you want to cancel this order?')) {
      this.cancelling.set(true);
      this.ordersService.cancelOrder(order.id, { reason: 'Cancelled by customer' }).subscribe({
        next: (result) => {
          if (result.success) this.loadOrder(order.id);
          else alert(result.errorMessage || 'Failed to cancel order');
          this.cancelling.set(false);
        },
        error: () => {
          alert('Failed to cancel order');
          this.cancelling.set(false);
        }
      });
    }
  }
}
