import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { VendorsService } from '@core/services/vendors.service';
import { AuthService } from '@core/auth/auth.service';
import { catchError, of } from 'rxjs';

interface VendorStats {
  totalSales: number;
  totalOrders: number;
  pendingOrders: number;
  totalProducts: number;
  averageRating: number;
  reviewCount: number;
  monthlyRevenue: number;
  revenueGrowth: number;
}

interface RecentOrder {
  id: string;
  orderNumber: string;
  customerName: string;
  total: number;
  status: string;
  createdAt: string;
}

@Component({
  selector: 'app-vendor-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './vendor-dashboard.component.html',
  styleUrl: './vendor-dashboard.component.css'
})
export class VendorDashboardComponent implements OnInit {
  private vendorsService = inject(VendorsService);
  private authService = inject(AuthService);
  
  sidebarOpen = signal(true);
  loading = signal(true);
  
  stats = signal<VendorStats>({
    totalSales: 0,
    totalOrders: 0,
    pendingOrders: 0,
    totalProducts: 0,
    averageRating: 0,
    reviewCount: 0,
    monthlyRevenue: 0,
    revenueGrowth: 0
  });
  
  recentOrders = signal<RecentOrder[]>([]);
  
  ngOnInit() {
    this.loadDashboard();
  }
  
  loadDashboard() {
    this.loading.set(true);
    
    // Simulate loading vendor dashboard data
    setTimeout(() => {
      this.stats.set({
        totalSales: 45230.50,
        totalOrders: 156,
        pendingOrders: 8,
        totalProducts: 42,
        averageRating: 4.7,
        reviewCount: 89,
        monthlyRevenue: 8750.00,
        revenueGrowth: 12.5
      });
      
      this.recentOrders.set([
        { id: '1', orderNumber: 'ORD-001', customerName: 'John Doe', total: 159.99, status: 'Pending', createdAt: '2024-03-15T10:00:00Z' },
        { id: '2', orderNumber: 'ORD-002', customerName: 'Jane Smith', total: 89.50, status: 'Processing', createdAt: '2024-03-14T15:30:00Z' },
        { id: '3', orderNumber: 'ORD-003', customerName: 'Bob Wilson', total: 245.00, status: 'Shipped', createdAt: '2024-03-13T09:00:00Z' },
        { id: '4', orderNumber: 'ORD-004', customerName: 'Alice Brown', total: 75.25, status: 'Pending', createdAt: '2024-03-12T14:00:00Z' }
      ]);
      
      this.loading.set(false);
    }, 500);
  }
}
