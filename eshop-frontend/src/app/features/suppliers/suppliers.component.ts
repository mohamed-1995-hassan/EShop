import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { SuppliersService, SupplierDto } from '@services/suppliers.service';

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './suppliers.component.html',
  styleUrl: './suppliers.component.css'
})
export class SuppliersComponent implements OnInit {
  private readonly suppliersService = inject(SuppliersService);
  
  suppliers = signal<SupplierDto[]>([]);
  loading = signal(true);
  searchQuery = '';
  
  activeSuppliers = () => this.suppliers().filter(s => s.status === 'Active').length;
  totalCountries = () => new Set(this.suppliers().map(s => s.country)).size;
  
  filteredSuppliers = () => {
    if (!this.searchQuery) return this.suppliers();
    const query = this.searchQuery.toLowerCase();
    return this.suppliers().filter(s => 
      s.name.toLowerCase().includes(query) || 
      s.country?.toLowerCase().includes(query)
    );
  };
  
  ngOnInit() {
    this.loadSuppliers();
  }
  
  loadSuppliers() {
    this.loading.set(true);
    
    this.suppliersService.getActiveSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers.set(suppliers);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.suppliers.set([
          { id: '1', name: 'TechSource Global', country: 'USA', status: 'Active', isVerified: true, email: 'contact@techsource.com', categories: ['Electronics', 'Accessories'] },
          { id: '2', name: 'EuroStyle Manufacturing', country: 'Germany', status: 'Active', isVerified: true, email: 'info@eurostyle.de', categories: ['Fashion', 'Apparel'] },
          { id: '3', name: 'Asia Pacific Trading', country: 'Singapore', status: 'Active', isVerified: true, email: 'sales@aptrade.sg', categories: ['General', 'Wholesale'] },
          { id: '4', name: 'Nordic Naturals', country: 'Sweden', status: 'Active', isVerified: false, email: 'hello@nordicnat.se', categories: ['Health', 'Wellness'] },
          { id: '5', name: 'Artisan Crafts Co', country: 'Italy', status: 'Active', isVerified: true, email: 'info@artisancrafts.it', categories: ['Home', 'Decor'] },
          { id: '6', name: 'Pacific Rim Imports', country: 'Japan', status: 'Active', isVerified: false, email: 'contact@pacrim.jp', categories: ['Electronics', 'Gadgets'] }
        ]);
      }
    });
  }
}
