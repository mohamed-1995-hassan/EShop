import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface CurrencyDto {
  id: string;
  code: string;
  name: string;
  symbol: string;
  nativeSymbol?: string;
  decimalPlaces: number;
  symbolFirst: boolean;
  decimalSeparator: string;
  thousandsSeparator: string;
  roundingRule: string;
  isPrimary: boolean;
  isActive: boolean;
  exchangeRates: ExchangeRateDto[];
}

export interface ExchangeRateDto {
  targetCurrencyCode: string;
  rate: number;
  source: string;
  updatedAt: string;
}

export interface CurrencyConversionResultDto {
  fromCurrency: string;
  toCurrency: string;
  originalAmount: number;
  convertedAmount: number;
  rate: number;
}

export interface CreateCurrencyRequest {
  code: string;
  name: string;
  symbol: string;
  decimalPlaces?: number;
  symbolFirst?: boolean;
  nativeSymbol?: string;
}

export interface SetExchangeRateRequest {
  targetCurrencyCode: string;
  rate: number;
  source?: 'Manual' | 'API' | 'Bank';
}

export interface SetFormattingRequest {
  decimalSeparator: string;
  thousandsSeparator: string;
  symbolFirst: boolean;
}

export interface SetRoundingRuleRequest {
  rule: 'None' | 'HalfUp' | 'HalfDown' | 'Up' | 'Down' | 'Nearest5' | 'Nearest10';
}

export interface SetActiveRequest {
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/currency`;

  // GET /api/currency/{id} - Get currency by ID
  getCurrency(id: string): Observable<CurrencyDto> {
    return this.http.get<CurrencyDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/currency/code/{code} - Get currency by code
  getCurrencyByCode(code: string): Observable<CurrencyDto> {
    return this.http.get<CurrencyDto>(`${this.baseUrl}/code/${code}`);
  }

  // GET /api/currency/primary - Get primary currency
  getPrimaryCurrency(): Observable<CurrencyDto> {
    return this.http.get<CurrencyDto>(`${this.baseUrl}/primary`);
  }

  // GET /api/currency/active - Get active currencies
  getActiveCurrencies(): Observable<CurrencyDto[]> {
    return this.http.get<CurrencyDto[]>(`${this.baseUrl}/active`);
  }

  // GET /api/currency/convert - Convert currency
  convert(from: string, to: string, amount: number): Observable<CurrencyConversionResultDto> {
    const params = new HttpParams()
      .set('from', from)
      .set('to', to)
      .set('amount', amount.toString());
    return this.http.get<CurrencyConversionResultDto>(`${this.baseUrl}/convert`, { params });
  }

  // GET /api/currency/format - Format amount
  format(code: string, amount: number): Observable<string> {
    const params = new HttpParams()
      .set('code', code)
      .set('amount', amount.toString());
    return this.http.get<string>(`${this.baseUrl}/format`, { params });
  }

  // POST /api/currency - Create currency
  createCurrency(request: CreateCurrencyRequest): Observable<string> {
    return this.http.post<string>(this.baseUrl, request);
  }

  // POST /api/currency/{currencyId}/exchange-rates - Set exchange rate
  setExchangeRate(currencyId: string, request: SetExchangeRateRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${currencyId}/exchange-rates`, request);
  }

  // POST /api/currency/{currencyId}/set-primary - Set as primary
  setPrimary(currencyId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${currencyId}/set-primary`, {});
  }

  // PUT /api/currency/{currencyId}/formatting - Set formatting
  setFormatting(currencyId: string, request: SetFormattingRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${currencyId}/formatting`, request);
  }

  // PUT /api/currency/{currencyId}/rounding - Set rounding rule
  setRoundingRule(currencyId: string, request: SetRoundingRuleRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${currencyId}/rounding`, request);
  }

  // PUT /api/currency/{currencyId}/active - Set active status
  setActive(currencyId: string, request: SetActiveRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${currencyId}/active`, request);
  }
}
