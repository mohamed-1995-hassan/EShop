import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface TaxConfigurationDto {
  id: string;
  type: TaxType;
  countryCode: string;
  regionCode?: string;
  name: string;
  defaultRate: number;
  isTaxIncludedInPrice: boolean;
  isActive: boolean;
  categoryRates: TaxCategoryRateDto[];
  exemptions: TaxExemptionDto[];
  createdAt: string;
}

export interface TaxCategoryRateDto {
  id: string;
  categoryId: string;
  categoryName: string;
  rate: number;
  description?: string;
}

export interface TaxExemptionDto {
  id: string;
  type: ExemptionType;
  entityId?: string;
  description: string;
  expiresAt?: string;
}

export interface TaxCalculationResultDto {
  taxAmount: number;
  taxRate: number;
  taxName: string;
  isExempt: boolean;
  exemptionReason?: string;
}

export type TaxType = 'VAT' | 'GST' | 'SalesTax';
export type ExemptionType = 'Customer' | 'Product' | 'Category' | 'Region';

export interface CalculateTaxRequest {
  countryCode: string;
  amount: number;
  currency: string;
  categoryId?: string;
  customerId?: string;
  isDigitalGood?: boolean;
  regionCode?: string;
}

export interface CreateVATRequest {
  countryCode: string;
  name: string;
  defaultRate: number;
  isTaxIncludedInPrice?: boolean;
  regionCode?: string;
}

export interface CreateGSTRequest {
  countryCode: string;
  name: string;
  defaultRate: number;
  isTaxIncludedInPrice?: boolean;
  regionCode?: string;
}

export interface CreateSalesTaxRequest {
  countryCode: string;
  regionCode: string;
  name: string;
  defaultRate: number;
}

export interface AddCategoryRateRequest {
  categoryId: string;
  categoryName: string;
  rate: number;
  description?: string;
}

export interface AddExemptionRequest {
  type: ExemptionType;
  entityId?: string;
  description: string;
  expiresAt?: string;
}

export interface UpdateRateRequest {
  newRate: number;
}

export interface SetActiveRequest {
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class TaxService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/tax`;

  // GET /api/tax/{id}
  getTaxConfiguration(id: string): Observable<TaxConfigurationDto> {
    return this.http.get<TaxConfigurationDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/tax/country/{countryCode}
  getTaxByCountry(countryCode: string, regionCode?: string): Observable<TaxConfigurationDto> {
    let params = new HttpParams();
    if (regionCode) params = params.set('regionCode', regionCode);
    return this.http.get<TaxConfigurationDto>(`${this.baseUrl}/country/${countryCode}`, { params });
  }

  // GET /api/tax/active
  getActiveTaxConfigurations(): Observable<TaxConfigurationDto[]> {
    return this.http.get<TaxConfigurationDto[]>(`${this.baseUrl}/active`);
  }

  // POST /api/tax/calculate
  calculateTax(request: CalculateTaxRequest): Observable<TaxCalculationResultDto> {
    return this.http.post<TaxCalculationResultDto>(`${this.baseUrl}/calculate`, request);
  }

  // POST /api/tax/vat - Create VAT configuration
  createVAT(request: CreateVATRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/vat`, request);
  }

  // POST /api/tax/gst - Create GST configuration
  createGST(request: CreateGSTRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/gst`, request);
  }

  // POST /api/tax/sales-tax - Create sales tax configuration
  createSalesTax(request: CreateSalesTaxRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/sales-tax`, request);
  }

  // POST /api/tax/{taxConfigId}/category-rates - Add category rate
  addCategoryRate(taxConfigId: string, request: AddCategoryRateRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${taxConfigId}/category-rates`, request);
  }

  // POST /api/tax/{taxConfigId}/exemptions - Add exemption
  addExemption(taxConfigId: string, request: AddExemptionRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${taxConfigId}/exemptions`, request);
  }

  // PUT /api/tax/{taxConfigId}/rate - Update default rate
  updateRate(taxConfigId: string, request: UpdateRateRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${taxConfigId}/rate`, request);
  }

  // PUT /api/tax/{taxConfigId}/active - Set active status
  setActive(taxConfigId: string, request: SetActiveRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${taxConfigId}/active`, request);
  }
}
