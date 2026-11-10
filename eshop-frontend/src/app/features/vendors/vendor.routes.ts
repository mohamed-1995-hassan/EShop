import { Routes } from '@angular/router';

export const VENDOR_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./vendor-dashboard/vendor-dashboard.component').then(m => m.VendorDashboardComponent),
    title: 'Vendor Dashboard - EShop'
  },
  {
    path: 'products',
    loadComponent: () => import('./vendor-products/vendor-products.component').then(m => m.VendorProductsComponent),
    title: 'My Products - EShop'
  },
  {
    path: 'orders',
    loadComponent: () => import('./vendor-orders/vendor-orders.component').then(m => m.VendorOrdersComponent),
    title: 'My Orders - EShop'
  },
  {
    path: 'settings',
    loadComponent: () => import('./vendor-settings/vendor-settings.component').then(m => m.VendorSettingsComponent),
    title: 'Store Settings - EShop'
  }
];
