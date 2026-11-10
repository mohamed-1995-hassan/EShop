import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { TaxService, TaxConfigurationDto, TaxCategoryRateDto, TaxExemptionDto, TaxType, ExemptionType } from '@core/services/tax.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-tax',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './tax.component.html',
  styleUrl: './tax.component.css'
})
export class TaxComponent implements OnInit {
  private taxService = inject(TaxService);
  private fb = inject(FormBuilder);
  
  sidebarOpen = signal(true);
  loading = signal(true);
  saving = signal(false);
  
  taxConfigurations = signal<TaxConfigurationDto[]>([]);
  selectedConfig = signal<TaxConfigurationDto | null>(null);
  editingConfig = signal<TaxConfigurationDto | null>(null);
  
  showCreateModal = signal(false);
  showDetailsModal = signal(false);
  showAddCategoryRateModal = signal(false);
  showAddExemptionModal = signal(false);
  
  searchQuery = '';
  filterType = '';
  filterStatus = '';
  
  taxTypes: TaxType[] = ['VAT', 'GST', 'SalesTax'];
  
  countries = [
    { code: 'US', name: 'United States', flag: '🇺🇸' },
    { code: 'GB', name: 'United Kingdom', flag: '🇬🇧' },
    { code: 'DE', name: 'Germany', flag: '🇩🇪' },
    { code: 'FR', name: 'France', flag: '🇫🇷' },
    { code: 'CA', name: 'Canada', flag: '🇨🇦' },
    { code: 'AU', name: 'Australia', flag: '🇦🇺' },
    { code: 'JP', name: 'Japan', flag: '🇯🇵' },
    { code: 'IN', name: 'India', flag: '🇮🇳' },
    { code: 'BR', name: 'Brazil', flag: '🇧🇷' },
    { code: 'AE', name: 'UAE', flag: '🇦🇪' }
  ];
  
  taxForm = this.fb.group({
    type: ['VAT' as TaxType, Validators.required],
    name: ['', Validators.required],
    defaultRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    countryCode: ['', Validators.required],
    regionCode: [''],
    isTaxIncludedInPrice: [false]
  });
  
  ngOnInit() {
    this.loadTaxConfigurations();
  }
  
  loadTaxConfigurations() {
    this.loading.set(true);
    this.taxService.getActiveTaxConfigurations().pipe(
      catchError(err => {
        console.error('Error loading tax configurations:', err);
        // Return mock data for demo
        return of(this.getMockData());
      })
    ).subscribe(configs => {
      this.taxConfigurations.set(configs);
      this.loading.set(false);
    });
  }
  
  filteredConfigurations() {
    return this.taxConfigurations().filter(config => {
      const matchesSearch = !this.searchQuery || 
        config.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        config.countryCode.toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchesType = !this.filterType || config.type === this.filterType;
      const matchesStatus = !this.filterStatus || 
        (this.filterStatus === 'active' && config.isActive) ||
        (this.filterStatus === 'inactive' && !config.isActive);
      return matchesSearch && matchesType && matchesStatus;
    });
  }
  
  getActiveCount(): number {
    return this.taxConfigurations().filter(c => c.isActive).length;
  }
  
  getUniqueCountries(): number {
    return new Set(this.taxConfigurations().map(c => c.countryCode)).size;
  }
  
  getTotalExemptions(): number {
    return this.taxConfigurations().reduce((sum, c) => sum + c.exemptions.length, 0);
  }
  
  getCountryFlag(code: string): string {
    return this.countries.find(c => c.code === code)?.flag || '🌍';
  }
  
  getTaxTypeDescription(type: TaxType): string {
    switch (type) {
      case 'VAT': return 'Value Added Tax';
      case 'GST': return 'Goods & Services Tax';
      case 'SalesTax': return 'Sales Tax';
      default: return '';
    }
  }
  
  viewDetails(config: TaxConfigurationDto) {
    this.selectedConfig.set(config);
    this.showDetailsModal.set(true);
  }
  
  editConfig(config: TaxConfigurationDto) {
    this.editingConfig.set(config);
    this.taxForm.patchValue({
      type: config.type,
      name: config.name,
      defaultRate: config.defaultRate,
      countryCode: config.countryCode,
      regionCode: config.regionCode || '',
      isTaxIncludedInPrice: config.isTaxIncludedInPrice
    });
    this.showCreateModal.set(true);
  }
  
  deleteConfig(config: TaxConfigurationDto) {
    if (confirm(`Are you sure you want to delete "${config.name}"?`)) {
      this.taxConfigurations.update(configs => configs.filter(c => c.id !== config.id));
    }
  }
  
  toggleStatus(config: TaxConfigurationDto) {
    this.taxService.setActive(config.id, { isActive: !config.isActive }).pipe(
      catchError(err => {
        console.error('Error toggling status:', err);
        return of(null);
      })
    ).subscribe(() => {
      this.taxConfigurations.update(configs => 
        configs.map(c => c.id === config.id ? { ...c, isActive: !c.isActive } : c)
      );
    });
  }
  
  closeModal() {
    this.showCreateModal.set(false);
    this.editingConfig.set(null);
    this.taxForm.reset({ type: 'VAT', isTaxIncludedInPrice: false });
  }
  
  saveTaxConfig() {
    if (this.taxForm.invalid) return;
    
    this.saving.set(true);
    const formValue = this.taxForm.value;
    
    // Simulate API call
    setTimeout(() => {
      if (this.editingConfig()) {
        this.taxConfigurations.update(configs =>
          configs.map(c => c.id === this.editingConfig()!.id 
            ? { ...c, ...formValue } as TaxConfigurationDto
            : c
          )
        );
      } else {
        const newConfig: TaxConfigurationDto = {
          id: crypto.randomUUID(),
          type: formValue.type as TaxType,
          name: formValue.name!,
          defaultRate: formValue.defaultRate!,
          countryCode: formValue.countryCode!,
          regionCode: formValue.regionCode || undefined,
          isTaxIncludedInPrice: formValue.isTaxIncludedInPrice!,
          isActive: true,
          categoryRates: [],
          exemptions: [],
          createdAt: new Date().toISOString()
        };
        this.taxConfigurations.update(configs => [...configs, newConfig]);
      }
      
      this.saving.set(false);
      this.closeModal();
    }, 500);
  }
  
  private getMockData(): TaxConfigurationDto[] {
    return [
      {
        id: '1',
        type: 'VAT',
        name: 'UK Standard VAT',
        countryCode: 'GB',
        defaultRate: 20,
        isTaxIncludedInPrice: true,
        isActive: true,
        categoryRates: [
          { id: '1', categoryId: 'food', categoryName: 'Food & Beverages', rate: 0, description: 'Zero-rated items' },
          { id: '2', categoryId: 'books', categoryName: 'Books & Publications', rate: 0 }
        ],
        exemptions: [
          { id: '1', type: 'Customer', description: 'Non-profit organizations', entityId: 'nonprofit' }
        ],
        createdAt: '2024-01-15T10:00:00Z'
      },
      {
        id: '2',
        type: 'VAT',
        name: 'Germany VAT',
        countryCode: 'DE',
        defaultRate: 19,
        isTaxIncludedInPrice: true,
        isActive: true,
        categoryRates: [
          { id: '3', categoryId: 'food', categoryName: 'Food', rate: 7 }
        ],
        exemptions: [],
        createdAt: '2024-01-15T10:00:00Z'
      },
      {
        id: '3',
        type: 'SalesTax',
        name: 'California Sales Tax',
        countryCode: 'US',
        regionCode: 'CA',
        defaultRate: 7.25,
        isTaxIncludedInPrice: false,
        isActive: true,
        categoryRates: [],
        exemptions: [
          { id: '2', type: 'Category', description: 'Groceries (unprepared food)', entityId: 'groceries' }
        ],
        createdAt: '2024-02-01T10:00:00Z'
      },
      {
        id: '4',
        type: 'GST',
        name: 'Australia GST',
        countryCode: 'AU',
        defaultRate: 10,
        isTaxIncludedInPrice: true,
        isActive: true,
        categoryRates: [
          { id: '4', categoryId: 'health', categoryName: 'Health & Medical', rate: 0, description: 'GST-free' }
        ],
        exemptions: [],
        createdAt: '2024-02-10T10:00:00Z'
      },
      {
        id: '5',
        type: 'GST',
        name: 'India GST',
        countryCode: 'IN',
        defaultRate: 18,
        isTaxIncludedInPrice: false,
        isActive: false,
        categoryRates: [
          { id: '5', categoryId: 'essentials', categoryName: 'Essential Items', rate: 5 },
          { id: '6', categoryId: 'luxury', categoryName: 'Luxury Goods', rate: 28 }
        ],
        exemptions: [],
        createdAt: '2024-03-01T10:00:00Z'
      }
    ];
  }
}
