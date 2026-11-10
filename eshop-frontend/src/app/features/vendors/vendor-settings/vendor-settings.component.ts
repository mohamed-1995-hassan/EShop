import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';

interface VendorProfile {
  storeName: string;
  slug: string;
  description: string;
  logo?: string;
  banner?: string;
  email: string;
  phone: string;
  address: string;
  commissionRate: number;
  payoutMethod: string;
  payoutDetails: string;
}

@Component({
  selector: 'app-vendor-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './vendor-settings.component.html',
  styleUrl: './vendor-settings.component.css'
})
export class VendorSettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  
  sidebarOpen = signal(true);
  loading = signal(true);
  saving = signal(false);
  
  profile = signal<VendorProfile>({
    storeName: '',
    slug: '',
    description: '',
    email: '',
    phone: '',
    address: '',
    commissionRate: 10,
    payoutMethod: 'bank',
    payoutDetails: ''
  });
  
  profileForm = this.fb.group({
    storeName: ['', Validators.required],
    slug: ['', Validators.required],
    description: ['']
  });
  
  notifications = [
    { key: 'newOrder', label: 'New Orders', description: 'Get notified when you receive a new order', enabled: true },
    { key: 'lowStock', label: 'Low Stock Alerts', description: 'Alert when product stock is running low', enabled: true },
    { key: 'reviews', label: 'New Reviews', description: 'Notification when customers leave reviews', enabled: false },
    { key: 'payouts', label: 'Payout Updates', description: 'Updates about your earnings and payouts', enabled: true }
  ];
  
  ngOnInit() {
    this.loadSettings();
  }
  
  loadSettings() {
    this.loading.set(true);
    setTimeout(() => {
      this.profile.set({
        storeName: 'Tech Gadgets Store',
        slug: 'tech-gadgets',
        description: 'Premium electronics and gadgets at affordable prices. We specialize in wireless audio, smart devices, and computer accessories.',
        logo: 'https://images.unsplash.com/photo-1560472354-b33ff0c44a43?w=100',
        email: 'contact@techgadgets.com',
        phone: '+1 (555) 123-4567',
        address: '123 Tech Street, San Francisco, CA 94105',
        commissionRate: 10,
        payoutMethod: 'bank',
        payoutDetails: '****4567'
      });
      
      this.profileForm.patchValue({
        storeName: this.profile().storeName,
        slug: this.profile().slug,
        description: this.profile().description
      });
      
      this.loading.set(false);
    }, 500);
  }
  
  saveProfile() {
    if (this.profileForm.invalid) return;
    this.saving.set(true);
    setTimeout(() => {
      this.saving.set(false);
    }, 500);
  }
  
  toggleNotification(key: string) {
    const notification = this.notifications.find(n => n.key === key);
    if (notification) {
      notification.enabled = !notification.enabled;
    }
  }
}
