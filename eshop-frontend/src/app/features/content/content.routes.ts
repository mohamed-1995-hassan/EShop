import { Routes } from '@angular/router';

export const CONTENT_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'pages',
    pathMatch: 'full'
  },
  {
    path: 'pages',
    loadComponent: () => import('./pages/pages.component').then(m => m.PagesComponent),
    title: 'Manage Pages - EShop'
  },
  {
    path: 'blog',
    loadComponent: () => import('./blog-management/blog-management.component').then(m => m.BlogManagementComponent),
    title: 'Manage Blog - EShop'
  },
  {
    path: 'faq',
    loadComponent: () => import('./faq-management/faq-management.component').then(m => m.FaqManagementComponent),
    title: 'Manage FAQ - EShop'
  },
  {
    path: 'banners',
    loadComponent: () => import('./banners/banners.component').then(m => m.BannersComponent),
    title: 'Manage Banners - EShop'
  }
];
