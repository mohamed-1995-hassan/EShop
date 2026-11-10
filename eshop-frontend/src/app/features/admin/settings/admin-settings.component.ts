import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './admin-settings.component.html',
  styleUrl: './admin-settings.component.css'
})
export class AdminSettingsComponent implements OnInit {
  activeSection = signal('general');
  
  sections = [
    { id: 'general', label: 'General', icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z M15 12a3 3 0 11-6 0 3 3 0 016 0z' },
    { id: 'currency', label: 'Currency & Tax', icon: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z' },
    { id: 'shipping', label: 'Shipping', icon: 'M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4' },
    { id: 'notifications', label: 'Notifications', icon: 'M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9' },
    { id: 'security', label: 'Security', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z' }
  ];
  
  settings = {
    storeName: 'EShop',
    storeDescription: 'Your one-stop shop for all your needs',
    contactEmail: 'support@eshop.com',
    contactPhone: '1-800-ESHOP',
    timezone: 'America/New_York',
    currency: 'USD',
    currencyPosition: 'before',
    taxEnabled: true,
    taxRate: 8.25,
    taxDisplay: 'excluded',
    freeShippingEnabled: true,
    freeShippingThreshold: 50,
    twoFactorEnabled: false,
    sessionTimeout: 30,
    requireUppercase: true,
    requireNumber: true,
    requireSpecialChar: false
  };
  
  shippingZones = [
    { name: 'Domestic', countries: ['USA'], rate: 5.99, estimatedDays: '3-5' },
    { name: 'Canada', countries: ['Canada'], rate: 12.99, estimatedDays: '5-7' },
    { name: 'International', countries: ['UK', 'Germany', 'France'], rate: 24.99, estimatedDays: '7-14' }
  ];
  
  notifications = [
    { key: 'orderConfirmation', label: 'Order Confirmation', description: 'Send email when order is placed', enabled: true },
    { key: 'orderShipped', label: 'Order Shipped', description: 'Send email when order is shipped', enabled: true },
    { key: 'orderDelivered', label: 'Order Delivered', description: 'Send email when order is delivered', enabled: true },
    { key: 'lowStock', label: 'Low Stock Alert', description: 'Notify admin when product stock is low', enabled: true },
    { key: 'newReview', label: 'New Review', description: 'Notify admin when new review is submitted', enabled: false }
  ];
  
  ngOnInit() {}
  
  saveSettings() {
    alert('Settings saved successfully!');
  }
}
