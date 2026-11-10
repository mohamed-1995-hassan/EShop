import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { VendorsService } from '@core/services/vendors.service';

interface VendorProduct {
  id: string;
  name: string;
  sku: string;
  price: number;
  stock: number;
  status: 'Active' | 'Draft' | 'OutOfStock';
  image?: string;
  sales: number;
  createdAt: string;
}

@Component({
  selector: 'app-vendor-products',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './vendor-products.component.html',
  styleUrl: './vendor-products.component.css'
})
export class VendorProductsComponent implements OnInit {
  private fb = inject(FormBuilder);
  
  sidebarOpen = signal(true);
  loading = signal(true);
  saving = signal(false);
  
  products = signal<VendorProduct[]>([]);
  editingProduct = signal<VendorProduct | null>(null);
  showCreateModal = signal(false);
  
  searchQuery = '';
  filterStatus = '';
  sortBy = 'newest';
  
  productForm = this.fb.group({
    name: ['', Validators.required],
    sku: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0)]],
    stock: [0, [Validators.required, Validators.min(0)]],
    status: ['Active'],
    description: ['']
  });
  
  ngOnInit() {
    this.loadProducts();
  }
  
  loadProducts() {
    this.loading.set(true);
    setTimeout(() => {
      this.products.set([
        { id: '1', name: 'Wireless Headphones Pro', sku: 'WHP-001', price: 129.99, stock: 45, status: 'Active', sales: 156, createdAt: '2024-01-15T10:00:00Z', image: 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=300' },
        { id: '2', name: 'Smart Watch Series 5', sku: 'SWS-005', price: 299.99, stock: 0, status: 'OutOfStock', sales: 89, createdAt: '2024-02-01T10:00:00Z', image: 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=300' },
        { id: '3', name: 'Bluetooth Speaker Mini', sku: 'BSM-003', price: 49.99, stock: 120, status: 'Active', sales: 234, createdAt: '2024-02-15T10:00:00Z' },
        { id: '4', name: 'USB-C Hub 7-in-1', sku: 'UCH-007', price: 59.99, stock: 78, status: 'Active', sales: 67, createdAt: '2024-03-01T10:00:00Z' },
        { id: '5', name: 'Mechanical Keyboard RGB', sku: 'MKR-010', price: 149.99, stock: 23, status: 'Draft', sales: 0, createdAt: '2024-03-10T10:00:00Z' }
      ]);
      this.loading.set(false);
    }, 500);
  }
  
  filteredProducts() {
    return this.products().filter(p => {
      const matchesSearch = !this.searchQuery || p.name.toLowerCase().includes(this.searchQuery.toLowerCase()) || p.sku.toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchesStatus = !this.filterStatus || p.status === this.filterStatus;
      return matchesSearch && matchesStatus;
    });
  }
  
  editProduct(product: VendorProduct) {
    this.editingProduct.set(product);
    this.productForm.patchValue({
      name: product.name,
      sku: product.sku,
      price: product.price,
      stock: product.stock,
      status: product.status === 'OutOfStock' ? 'Active' : product.status
    });
    this.showCreateModal.set(true);
  }
  
  toggleStatus(product: VendorProduct) {
    const newStatus = product.status === 'Active' ? 'Draft' : 'Active';
    this.products.update(products => products.map(p => p.id === product.id ? { ...p, status: newStatus as any } : p));
  }
  
  closeModal() {
    this.showCreateModal.set(false);
    this.editingProduct.set(null);
    this.productForm.reset({ status: 'Active', price: 0, stock: 0 });
  }
  
  saveProduct() {
    if (this.productForm.invalid) return;
    this.saving.set(true);
    setTimeout(() => {
      this.saving.set(false);
      this.closeModal();
      this.loadProducts();
    }, 500);
  }
}
