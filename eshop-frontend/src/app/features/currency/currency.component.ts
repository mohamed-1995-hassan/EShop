import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { CurrencyService, CurrencyDto, CurrencyConversionResultDto } from '@core/services/currency.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-currency',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './currency.component.html',
  styleUrl: './currency.component.css'
})
export class CurrencyComponent implements OnInit {
  private currencyService = inject(CurrencyService);
  private fb = inject(FormBuilder);
  
  sidebarOpen = signal(true);
  loading = signal(true);
  saving = signal(false);
  
  currencies = signal<CurrencyDto[]>([]);
  primaryCurrency = signal<CurrencyDto | null>(null);
  selectedCurrency = signal<CurrencyDto | null>(null);
  editingCurrency = signal<CurrencyDto | null>(null);
  conversionResult = signal<CurrencyConversionResultDto | null>(null);
  
  showCreateModal = signal(false);
  showRatesModal = signal(false);
  
  convertAmount = 100;
  convertFrom = 'USD';
  convertTo = 'EUR';
  newRateTarget = '';
  newRateValue = 0;
  
  currencyForm = this.fb.group({
    code: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    name: ['', Validators.required],
    symbol: ['', Validators.required],
    decimalPlaces: [2],
    symbolFirst: [true],
    nativeSymbol: ['']
  });
  
  ngOnInit() {
    this.loadCurrencies();
  }
  
  loadCurrencies() {
    this.loading.set(true);
    this.currencyService.getActiveCurrencies().pipe(
      catchError(err => {
        console.error('Error loading currencies:', err);
        return of(this.getMockData());
      })
    ).subscribe(currencies => {
      this.currencies.set(currencies);
      const primary = currencies.find(c => c.isPrimary);
      this.primaryCurrency.set(primary || null);
      if (currencies.length > 0) {
        this.convertFrom = currencies[0].code;
        this.convertTo = currencies.length > 1 ? currencies[1].code : currencies[0].code;
      }
      this.loading.set(false);
    });
  }
  
  getCurrencyFlag(code: string): string {
    const flags: Record<string, string> = {
      'USD': '🇺🇸', 'EUR': '🇪🇺', 'GBP': '🇬🇧', 'JPY': '🇯🇵', 'CAD': '🇨🇦',
      'AUD': '🇦🇺', 'CHF': '🇨🇭', 'CNY': '🇨🇳', 'INR': '🇮🇳', 'AED': '🇦🇪',
      'SAR': '🇸🇦', 'BRL': '🇧🇷', 'MXN': '🇲🇽', 'SGD': '🇸🇬', 'HKD': '🇭🇰'
    };
    return flags[code] || '💰';
  }
  
  getFormatExample(currency: CurrencyDto): string {
    const amount = currency.symbolFirst 
      ? `${currency.symbol}1${currency.thousandsSeparator}234${currency.decimalSeparator}${'0'.repeat(currency.decimalPlaces)}`
      : `1${currency.thousandsSeparator}234${currency.decimalSeparator}${'0'.repeat(currency.decimalPlaces)}${currency.symbol}`;
    return amount;
  }
  
  swapCurrencies() {
    const temp = this.convertFrom;
    this.convertFrom = this.convertTo;
    this.convertTo = temp;
  }
  
  convertCurrency() {
    if (this.convertFrom === this.convertTo) {
      this.conversionResult.set({
        fromCurrency: this.convertFrom,
        toCurrency: this.convertTo,
        originalAmount: this.convertAmount,
        convertedAmount: this.convertAmount,
        rate: 1
      });
      return;
    }
    
    this.currencyService.convert(this.convertFrom, this.convertTo, this.convertAmount).pipe(
      catchError(err => {
        console.error('Error converting currency:', err);
        const mockRate = this.getMockRate(this.convertFrom, this.convertTo);
        return of({
          fromCurrency: this.convertFrom,
          toCurrency: this.convertTo,
          originalAmount: this.convertAmount,
          convertedAmount: this.convertAmount * mockRate,
          rate: mockRate
        });
      })
    ).subscribe(result => {
      this.conversionResult.set(result);
    });
  }
  
  private getMockRate(from: string, to: string): number {
    const rates: Record<string, number> = {
      'USD-EUR': 0.92, 'USD-GBP': 0.79, 'USD-JPY': 149.50, 'USD-CAD': 1.36,
      'EUR-USD': 1.09, 'EUR-GBP': 0.86, 'GBP-USD': 1.27, 'GBP-EUR': 1.16
    };
    return rates[`${from}-${to}`] || 1;
  }
  
  editCurrency(currency: CurrencyDto) {
    this.editingCurrency.set(currency);
    this.currencyForm.patchValue({
      code: currency.code,
      name: currency.name,
      symbol: currency.symbol,
      decimalPlaces: currency.decimalPlaces,
      symbolFirst: currency.symbolFirst,
      nativeSymbol: currency.nativeSymbol || ''
    });
    this.showCreateModal.set(true);
  }
  
  manageRates(currency: CurrencyDto) {
    this.selectedCurrency.set(currency);
    this.showRatesModal.set(true);
  }
  
  deleteCurrency(currency: CurrencyDto) {
    if (confirm(`Are you sure you want to delete ${currency.name}?`)) {
      this.currencies.update(currencies => currencies.filter(c => c.id !== currency.id));
    }
  }
  
  setPrimary(currency: CurrencyDto) {
    this.currencyService.setPrimary(currency.id).pipe(
      catchError(err => {
        console.error('Error setting primary currency:', err);
        return of(null);
      })
    ).subscribe(() => {
      this.currencies.update(currencies =>
        currencies.map(c => ({ ...c, isPrimary: c.id === currency.id }))
      );
      this.primaryCurrency.set(currency);
    });
  }
  
  getOtherCurrencies(): CurrencyDto[] {
    return this.currencies().filter(c => c.id !== this.selectedCurrency()?.id);
  }
  
  addExchangeRate() {
    if (!this.newRateTarget || !this.newRateValue) return;
    
    const currency = this.selectedCurrency();
    if (!currency) return;
    
    this.selectedCurrency.update(c => c ? {
      ...c,
      exchangeRates: [
        ...c.exchangeRates,
        {
          targetCurrencyCode: this.newRateTarget,
          rate: this.newRateValue,
          source: 'Manual',
          updatedAt: new Date().toISOString()
        }
      ]
    } : null);
    
    this.newRateTarget = '';
    this.newRateValue = 0;
  }
  
  closeModal() {
    this.showCreateModal.set(false);
    this.editingCurrency.set(null);
    this.currencyForm.reset({ decimalPlaces: 2, symbolFirst: true });
  }
  
  saveCurrency() {
    if (this.currencyForm.invalid) return;
    
    this.saving.set(true);
    const formValue = this.currencyForm.value;
    
    setTimeout(() => {
      if (this.editingCurrency()) {
        this.currencies.update(currencies =>
          currencies.map(c => c.id === this.editingCurrency()!.id 
            ? { ...c, ...formValue } as CurrencyDto
            : c
          )
        );
      } else {
        const newCurrency: CurrencyDto = {
          id: crypto.randomUUID(),
          code: formValue.code!.toUpperCase(),
          name: formValue.name!,
          symbol: formValue.symbol!,
          nativeSymbol: formValue.nativeSymbol || undefined,
          decimalPlaces: formValue.decimalPlaces!,
          symbolFirst: formValue.symbolFirst!,
          decimalSeparator: '.',
          thousandsSeparator: ',',
          roundingRule: 'HalfUp',
          isPrimary: this.currencies().length === 0,
          isActive: true,
          exchangeRates: []
        };
        this.currencies.update(currencies => [...currencies, newCurrency]);
      }
      
      this.saving.set(false);
      this.closeModal();
    }, 500);
  }
  
  private getMockData(): CurrencyDto[] {
    return [
      {
        id: '1', code: 'USD', name: 'US Dollar', symbol: '$', nativeSymbol: 'US$',
        decimalPlaces: 2, symbolFirst: true, decimalSeparator: '.', thousandsSeparator: ',',
        roundingRule: 'HalfUp', isPrimary: true, isActive: true,
        exchangeRates: [
          { targetCurrencyCode: 'EUR', rate: 0.92, source: 'API', updatedAt: '2024-03-15T10:00:00Z' },
          { targetCurrencyCode: 'GBP', rate: 0.79, source: 'API', updatedAt: '2024-03-15T10:00:00Z' },
          { targetCurrencyCode: 'JPY', rate: 149.50, source: 'API', updatedAt: '2024-03-15T10:00:00Z' },
          { targetCurrencyCode: 'CAD', rate: 1.36, source: 'API', updatedAt: '2024-03-15T10:00:00Z' }
        ]
      },
      {
        id: '2', code: 'EUR', name: 'Euro', symbol: '€', decimalPlaces: 2, symbolFirst: false,
        decimalSeparator: ',', thousandsSeparator: '.', roundingRule: 'HalfUp',
        isPrimary: false, isActive: true,
        exchangeRates: [
          { targetCurrencyCode: 'USD', rate: 1.09, source: 'API', updatedAt: '2024-03-15T10:00:00Z' },
          { targetCurrencyCode: 'GBP', rate: 0.86, source: 'API', updatedAt: '2024-03-15T10:00:00Z' }
        ]
      },
      {
        id: '3', code: 'GBP', name: 'British Pound', symbol: '£', decimalPlaces: 2, symbolFirst: true,
        decimalSeparator: '.', thousandsSeparator: ',', roundingRule: 'HalfUp',
        isPrimary: false, isActive: true,
        exchangeRates: [
          { targetCurrencyCode: 'USD', rate: 1.27, source: 'API', updatedAt: '2024-03-15T10:00:00Z' },
          { targetCurrencyCode: 'EUR', rate: 1.16, source: 'API', updatedAt: '2024-03-15T10:00:00Z' }
        ]
      },
      {
        id: '4', code: 'JPY', name: 'Japanese Yen', symbol: '¥', decimalPlaces: 0, symbolFirst: true,
        decimalSeparator: '.', thousandsSeparator: ',', roundingRule: 'Down',
        isPrimary: false, isActive: true,
        exchangeRates: [
          { targetCurrencyCode: 'USD', rate: 0.0067, source: 'API', updatedAt: '2024-03-15T10:00:00Z' }
        ]
      },
      {
        id: '5', code: 'AED', name: 'UAE Dirham', symbol: 'د.إ', nativeSymbol: 'AED',
        decimalPlaces: 2, symbolFirst: false, decimalSeparator: '.', thousandsSeparator: ',',
        roundingRule: 'HalfUp', isPrimary: false, isActive: false, exchangeRates: []
      }
    ];
  }
}
