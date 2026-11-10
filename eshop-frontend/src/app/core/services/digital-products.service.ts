import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface DigitalProductDto {
  id: string;
  productId: string;
  name: string;
  description: string;
  type: DigitalProductType;
  files: DigitalFileDto[];
  maxDownloads?: number;
  accessDurationDays?: number;
  licenseType?: LicenseType;
  maxDevices?: number;
  allowWatermarking?: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface DigitalProductSummaryDto {
  id: string;
  productId: string;
  name: string;
  type: DigitalProductType;
  fileCount: number;
  isActive: boolean;
}

export interface DigitalFileDto {
  id: string;
  fileName: string;
  fileUrl: string;
  fileSizeBytes: number;
  contentType: string;
  version: string;
  checksum?: string;
  fileType: FileType;
}

export interface LicenseKeyDto {
  id: string;
  key: string;
  customerId?: string;
  orderId?: string;
  status: string;
  activationsUsed: number;
  maxActivations: number;
  expiresAt?: string;
  assignedAt?: string;
  revokedAt?: string;
}

export type DigitalProductType = 'Downloadable' | 'Software' | 'Ebook' | 'Course';
export type LicenseType = 'SingleUse' | 'Subscription' | 'Perpetual' | 'Floating';
export type FileType = 'Primary' | 'Sample' | 'Documentation' | 'Update';

export interface CreateDownloadableRequest {
  productId: string;
  name: string;
  description: string;
  maxDownloads?: number;
  accessDurationDays?: number;
}

export interface CreateSoftwareRequest {
  productId: string;
  name: string;
  description: string;
  licenseType: LicenseType;
  maxDevices?: number;
}

export interface CreateEbookRequest {
  productId: string;
  name: string;
  description: string;
  allowWatermarking?: boolean;
}

export interface AddFileRequest {
  fileName: string;
  fileUrl: string;
  fileSizeBytes: number;
  contentType: string;
  version?: string;
  checksum?: string;
  fileType?: FileType;
}

export interface AddLicenseRequest {
  key: string;
  expiresAt?: string;
}

export interface ImportLicensesRequest {
  keys: string[];
  expiresAt?: string;
}

export interface AssignLicenseRequest {
  customerId: string;
  orderId: string;
}

export interface RevokeLicenseRequest {
  reason: string;
}

export interface ValidateLicenseRequest {
  key: string;
  deviceId?: string;
}

export interface LicenseValidationResult {
  isValid: boolean;
  license?: LicenseKeyDto;
  errorMessage?: string;
}

@Injectable({ providedIn: 'root' })
export class DigitalProductsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/digital-products`;

  // GET /api/digital-products/{id}
  getDigitalProduct(id: string): Observable<DigitalProductDto> {
    return this.http.get<DigitalProductDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/digital-products/product/{productId}
  getByProductId(productId: string): Observable<DigitalProductDto> {
    return this.http.get<DigitalProductDto>(`${this.baseUrl}/product/${productId}`);
  }

  // GET /api/digital-products/type/{type}
  getByType(type: DigitalProductType): Observable<DigitalProductSummaryDto[]> {
    return this.http.get<DigitalProductSummaryDto[]>(`${this.baseUrl}/type/${type}`);
  }

  // GET /api/digital-products/active
  getActive(): Observable<DigitalProductSummaryDto[]> {
    return this.http.get<DigitalProductSummaryDto[]>(`${this.baseUrl}/active`);
  }

  // GET /api/digital-products/licenses/customer/{customerId}
  getCustomerLicenses(customerId: string): Observable<LicenseKeyDto[]> {
    return this.http.get<LicenseKeyDto[]>(`${this.baseUrl}/licenses/customer/${customerId}`);
  }

  // GET /api/digital-products/licenses/lookup/{key}
  lookupLicense(key: string): Observable<LicenseKeyDto> {
    return this.http.get<LicenseKeyDto>(`${this.baseUrl}/licenses/lookup/${key}`);
  }

  // POST /api/digital-products/downloadable
  createDownloadable(request: CreateDownloadableRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/downloadable`, request);
  }

  // POST /api/digital-products/software
  createSoftware(request: CreateSoftwareRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/software`, request);
  }

  // POST /api/digital-products/ebook
  createEbook(request: CreateEbookRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/ebook`, request);
  }

  // POST /api/digital-products/{digitalProductId}/files
  addFile(digitalProductId: string, request: AddFileRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${digitalProductId}/files`, request);
  }

  // POST /api/digital-products/{digitalProductId}/licenses
  addLicense(digitalProductId: string, request: AddLicenseRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${digitalProductId}/licenses`, request);
  }

  // POST /api/digital-products/{digitalProductId}/licenses/import
  importLicenses(digitalProductId: string, request: ImportLicensesRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${digitalProductId}/licenses/import`, request);
  }

  // POST /api/digital-products/{digitalProductId}/licenses/assign
  assignLicense(digitalProductId: string, request: AssignLicenseRequest): Observable<LicenseKeyDto> {
    return this.http.post<LicenseKeyDto>(`${this.baseUrl}/${digitalProductId}/licenses/assign`, request);
  }

  // POST /api/digital-products/{digitalProductId}/licenses/{licenseId}/revoke
  revokeLicense(digitalProductId: string, licenseId: string, request: RevokeLicenseRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${digitalProductId}/licenses/${licenseId}/revoke`, request);
  }

  // POST /api/digital-products/{digitalProductId}/licenses/validate
  validateLicense(digitalProductId: string, request: ValidateLicenseRequest): Observable<LicenseValidationResult> {
    return this.http.post<LicenseValidationResult>(`${this.baseUrl}/${digitalProductId}/licenses/validate`, request);
  }

  // POST /api/digital-products/{digitalProductId}/activate
  activate(digitalProductId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${digitalProductId}/activate`, {});
  }

  // POST /api/digital-products/{digitalProductId}/deactivate
  deactivate(digitalProductId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${digitalProductId}/deactivate`, {});
  }
}
