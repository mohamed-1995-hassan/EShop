import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent],
  templateUrl: './analytics.component.html',
  styleUrl: './analytics.component.css'
})
export class AnalyticsComponent implements OnInit {
  metrics = [
    { label: 'Total Revenue', value: '$124,563', change: 12.5, changePositive: true, icon: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z', iconBg: 'bg-green-100', iconColor: 'text-green-600' },
    { label: 'Orders', value: '1,847', change: 8.2, changePositive: true, icon: 'M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z', iconBg: 'bg-blue-100', iconColor: 'text-blue-600' },
    { label: 'Customers', value: '3,249', change: 5.1, changePositive: true, icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z', iconBg: 'bg-purple-100', iconColor: 'text-purple-600' },
    { label: 'Avg Order Value', value: '$67.43', change: -2.4, changePositive: false, icon: 'M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z', iconBg: 'bg-yellow-100', iconColor: 'text-yellow-600' }
  ];
  
  topProducts = [
    { name: 'Wireless Headphones Pro', sales: 342, revenue: 27359 },
    { name: 'Smart Watch Series 5', sales: 256, revenue: 51199 },
    { name: 'Laptop Stand Adjustable', sales: 198, revenue: 7920 },
    { name: 'USB-C Hub 7-in-1', sales: 187, revenue: 8415 },
    { name: 'Mechanical Keyboard RGB', sales: 156, revenue: 15599 }
  ];
  
  trafficSources = [
    { name: 'Direct', percentage: 35, color: 'bg-blue-600' },
    { name: 'Organic Search', percentage: 28, color: 'bg-green-600' },
    { name: 'Social Media', percentage: 18, color: 'bg-purple-600' },
    { name: 'Email', percentage: 12, color: 'bg-yellow-600' },
    { name: 'Referral', percentage: 7, color: 'bg-pink-600' }
  ];
  
  recentActivity = [
    { message: 'New order #12847 received', time: '2 minutes ago', icon: 'M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z', iconBg: 'bg-blue-100', iconColor: 'text-blue-600' },
    { message: 'Customer John D. signed up', time: '15 minutes ago', icon: 'M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z', iconBg: 'bg-green-100', iconColor: 'text-green-600' },
    { message: 'Product "Headphones" low stock', time: '1 hour ago', icon: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z', iconBg: 'bg-yellow-100', iconColor: 'text-yellow-600' },
    { message: 'Payment of $234.50 received', time: '2 hours ago', icon: 'M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z', iconBg: 'bg-purple-100', iconColor: 'text-purple-600' }
  ];
  
  funnelSteps = [
    { name: 'Visitors', value: 45678, conversionRate: 42, bg: 'bg-blue-100', text: 'text-blue-700' },
    { name: 'Product Views', value: 19185, conversionRate: 28, bg: 'bg-indigo-100', text: 'text-indigo-700' },
    { name: 'Add to Cart', value: 5372, conversionRate: 65, bg: 'bg-purple-100', text: 'text-purple-700' },
    { name: 'Checkout', value: 3492, conversionRate: 78, bg: 'bg-pink-100', text: 'text-pink-700' },
    { name: 'Purchase', value: 2724, conversionRate: 100, bg: 'bg-green-100', text: 'text-green-700' }
  ];
  
  ngOnInit() {}
}
