import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

// DTOs
export interface GeneralSettingsDto {
  storeName: string;
  storeEmail: string;
  storePhone?: string;
  storeAddress?: string;
  logoUrl?: string;
  faviconUrl?: string;
  defaultCurrency: string;
  defaultLanguage: string;
  timezone: string;
  dateFormat: string;
  timeFormat: string;
  weightUnit: 'kg' | 'lb';
  dimensionUnit: 'cm' | 'in';
}

export interface PaymentSettingsDto {
  enableStripe: boolean;
  stripePublishableKey?: string;
  enablePayPal: boolean;
  payPalClientId?: string;
  enableCOD: boolean;
  codFee?: number;
  enableBankTransfer: boolean;
  bankDetails?: string;
}

export interface ShippingSettingsDto {
  freeShippingThreshold?: number;
  defaultShippingCost: number;
  enableFlatRate: boolean;
  flatRateCost?: number;
  enableWeightBased: boolean;
  weightBasedRates?: WeightBasedRate[];
  enableLocalPickup: boolean;
  pickupAddress?: string;
}

export interface WeightBasedRate {
  minWeight: number;
  maxWeight: number;
  cost: number;
}

export interface EmailSettingsDto {
  smtpHost: string;
  smtpPort: number;
  smtpUsername: string;
  smtpPassword?: string;
  smtpSecure: boolean;
  fromEmail: string;
  fromName: string;
  enableOrderNotifications: boolean;
  enableShippingNotifications: boolean;
  enableMarketingEmails: boolean;
}

export interface UpdateGeneralSettingsRequest {
  storeName: string;
  storeEmail: string;
  storePhone?: string;
  storeAddress?: string;
  logoUrl?: string;
  defaultCurrency: string;
  defaultLanguage: string;
  timezone: string;
}

export interface UpdatePaymentSettingsRequest {
  enableStripe: boolean;
  stripePublishableKey?: string;
  stripeSecretKey?: string;
  enablePayPal: boolean;
  payPalClientId?: string;
  payPalSecret?: string;
  enableCOD: boolean;
  codFee?: number;
}

@Injectable({ providedIn: 'root' })
export class AdminSettingsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin/settings`;

  // GET /api/admin/settings/general
  getGeneralSettings(): Observable<GeneralSettingsDto> {
    return this.http.get<GeneralSettingsDto>(`${this.baseUrl}/general`);
  }

  // PUT /api/admin/settings/general
  updateGeneralSettings(request: UpdateGeneralSettingsRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/general`, request);
  }

  // GET /api/admin/settings/payment
  getPaymentSettings(): Observable<PaymentSettingsDto> {
    return this.http.get<PaymentSettingsDto>(`${this.baseUrl}/payment`);
  }

  // PUT /api/admin/settings/payment
  updatePaymentSettings(request: UpdatePaymentSettingsRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/payment`, request);
  }

  // GET /api/admin/settings/shipping
  getShippingSettings(): Observable<ShippingSettingsDto> {
    return this.http.get<ShippingSettingsDto>(`${this.baseUrl}/shipping`);
  }

  // PUT /api/admin/settings/shipping
  updateShippingSettings(settings: Partial<ShippingSettingsDto>): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/shipping`, settings);
  }

  // GET /api/admin/settings/email
  getEmailSettings(): Observable<EmailSettingsDto> {
    return this.http.get<EmailSettingsDto>(`${this.baseUrl}/email`);
  }

  // PUT /api/admin/settings/email
  updateEmailSettings(settings: Partial<EmailSettingsDto>): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/email`, settings);
  }
}
