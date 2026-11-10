import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { VendorsService, VendorSummaryDto, VendorDto } from '@services/vendors.service';

@Component({
  selector: 'app-vendors',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './vendors.component.html',
  styleUrl: './vendors.component.css'
})
export class VendorsComponent implements OnInit {
  private readonly vendorsService = inject(VendorsService);
  
  vendors = signal<VendorSummaryDto[]>([]);
  loading = signal(true);
  searchQuery = '';
  sortBy = 'name';
  
  filteredVendors = signal<VendorSummaryDto[]>([]);
  
  ngOnInit() {
    this.loadVendors();
  }
  
  loadVendors() {
    this.loading.set(true);
    
    this.vendorsService.getActiveVendors().subscribe({
      next: (vendors) => {
        this.vendors.set(vendors);
        this.filteredVendors.set(vendors);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        const mockVendors: VendorSummaryDto[] = [
          { id: '1', name: 'TechGear Pro', rating: 4.8, reviewCount: 2543, productCount: 156, isVerified: true, categories: ['Electronics', 'Accessories'] },
          { id: '2', name: 'Home Essentials Co', rating: 4.6, reviewCount: 1876, productCount: 234, isVerified: true, categories: ['Home', 'Kitchen'] },
          { id: '3', name: 'Fashion Forward', rating: 4.5, reviewCount: 3210, productCount: 412, isVerified: true, categories: ['Clothing', 'Accessories'] },
          { id: '4', name: 'Outdoor Adventures', rating: 4.7, reviewCount: 987, productCount: 89, isVerified: false, categories: ['Sports', 'Outdoor'] },
          { id: '5', name: 'Beauty & Beyond', rating: 4.4, reviewCount: 1543, productCount: 178, isVerified: true, categories: ['Beauty', 'Health'] },
          { id: '6', name: 'Pet Paradise', rating: 4.9, reviewCount: 654, productCount: 67, isVerified: false, categories: ['Pets'] }
        ];
        this.vendors.set(mockVendors);
        this.filteredVendors.set(mockVendors);
      }
    });
  }
  
  filterVendors() {
    let result = [...this.vendors()];
    
    if (this.searchQuery) {
      const query = this.searchQuery.toLowerCase();
      result = result.filter(v => v.name.toLowerCase().includes(query));
    }
    
    switch (this.sortBy) {
      case 'rating':
        result.sort((a, b) => b.rating - a.rating);
        break;
      case 'products':
        result.sort((a, b) => b.productCount - a.productCount);
        break;
      default:
        result.sort((a, b) => a.name.localeCompare(b.name));
    }
    
    this.filteredVendors.set(result);
  }
}
