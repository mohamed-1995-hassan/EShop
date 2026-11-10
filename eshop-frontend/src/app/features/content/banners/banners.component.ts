import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';

interface Banner {
  id: string;
  title: string;
  subtitle?: string;
  imageUrl: string;
  linkUrl?: string;
  position: string;
  isActive: boolean;
  startDate?: string;
  endDate?: string;
  sortOrder: number;
}

@Component({
  selector: 'app-banners',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './banners.component.html',
  styleUrl: './banners.component.css'
})
export class BannersComponent implements OnInit {
  private fb = inject(FormBuilder);
  sidebarOpen = signal(true);
  loading = signal(true);
  saving = signal(false);
  banners = signal<Banner[]>([]);
  editingBanner = signal<Banner | null>(null);
  showCreateModal = signal(false);
  filterPosition = signal('');
  positions = [{ value: '', label: 'All' }, { value: 'Hero', label: 'Hero' }, { value: 'Sidebar', label: 'Sidebar' }, { value: 'Footer', label: 'Footer' }, { value: 'Category', label: 'Category' }];
  bannerForm = this.fb.group({ title: ['', Validators.required], subtitle: [''], imageUrl: ['', Validators.required], linkUrl: [''], position: ['Hero'], sortOrder: [0], isActive: [true] });
  ngOnInit() { this.loadBanners(); }
  loadBanners() { this.loading.set(true); setTimeout(() => { this.banners.set([{ id: '1', title: 'Summer Sale', subtitle: 'Up to 50% off', imageUrl: 'https://images.unsplash.com/photo-1607082349566-187342175e2f?w=800', linkUrl: '/products?sale=summer', position: 'Hero', isActive: true, sortOrder: 1 }, { id: '2', title: 'New Arrivals', subtitle: 'Shop the latest', imageUrl: 'https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=800', linkUrl: '/products?new=true', position: 'Hero', isActive: true, sortOrder: 2 }, { id: '3', title: 'Free Shipping', imageUrl: 'https://images.unsplash.com/photo-1566576912321-d58ddd7a6088?w=400', position: 'Sidebar', isActive: true, sortOrder: 1 }]); this.loading.set(false); }, 500); }
  filteredBanners() { return this.banners().filter(b => !this.filterPosition() || b.position === this.filterPosition()); }
  editBanner(banner: Banner) { this.editingBanner.set(banner); this.bannerForm.patchValue(banner); this.showCreateModal.set(true); }
  toggleStatus(banner: Banner) { this.banners.update(banners => banners.map(b => b.id === banner.id ? { ...b, isActive: !b.isActive } : b)); }
  deleteBanner(banner: Banner) { if (confirm('Delete this banner?')) { this.banners.update(banners => banners.filter(b => b.id !== banner.id)); } }
  closeModal() { this.showCreateModal.set(false); this.editingBanner.set(null); this.bannerForm.reset({ position: 'Hero', sortOrder: 0, isActive: true }); }
  saveBanner() { if (this.bannerForm.invalid) return; this.saving.set(true); setTimeout(() => { this.saving.set(false); this.closeModal(); this.loadBanners(); }, 500); }
}
