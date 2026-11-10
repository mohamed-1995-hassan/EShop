import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { AdminDashboardService, DashboardOverviewDto, TopProductDto } from '@core/services/admin/admin-dashboard.service';
import { OrdersService, OrderSummaryDto } from '@core/services/orders.service';
import { DashboardStats, TopProduct, Order } from '@shared/models';
import { forkJoin, catchError, of } from 'rxjs';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, NavComponent],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit {
  private dashboardService = inject(AdminDashboardService);
  
  sidebarOpen = signal(true);
  stats = signal<DashboardStats | null>(null);
  recentOrders = signal<Order[]>([]);
  topProducts = signal<TopProduct[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  
  ngOnInit() {
    this.loadDashboardData();
  }
  
  private loadDashboardData() {
    this.loading.set(true);
    this.error.set(null);
    
    forkJoin({
      overview: this.dashboardService.getOverview().pipe(
        catchError(err => {
          console.error('Error loading overview:', err);
          return of(null);
        })
      ),
      topProducts: this.dashboardService.getTopProducts(5).pipe(
        catchError(err => {
          console.error('Error loading top products:', err);
          return of([]);
        })
      )
    }).subscribe({
      next: ({ overview, topProducts }) => {
        if (overview) {
          this.stats.set({
            totalRevenue: overview.totalRevenue,
            revenueGrowth: overview.revenueGrowth,
            totalOrders: overview.totalOrders,
            ordersGrowth: overview.ordersGrowth,
            totalCustomers: overview.totalCustomers,
            customersGrowth: overview.customersGrowth,
            averageOrderValue: overview.averageOrderValue,
            aovGrowth: overview.aovGrowth,
            conversionRate: 0,
            conversionGrowth: 0
          });
        }
        
        this.topProducts.set(topProducts.map(p => ({
          productId: p.productId,
          productName: p.productName,
          productImage: p.productImage,
          quantity: p.quantitySold,
          revenue: p.revenue
        })));
        
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading dashboard:', err);
        this.error.set('Failed to load dashboard data');
        this.loading.set(false);
      }
    });
  }
}
