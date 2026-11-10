import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface VendorDto {
  id: string;
  businessName: string;
  legalName: string;
  contactEmail: string;
  contactPhone: string;
  address: VendorAddressDto;
  taxId?: string;
  status: VendorStatus;
  tier: VendorTier;
  commissionRate: number;
  categories: VendorCategoryDto[];
  documents: VendorDocumentDto[];
  performance: VendorPerformanceDto;
  createdAt: string;
  approvedAt?: string;
}

export interface VendorAddressDto {
  street: string;
  city: string;
  country: string;
  postalCode: string;
}

export interface VendorCategoryDto {
  id: string;
  categoryId: string;
  categoryName: string;
  customCommissionRate?: number;
}

export interface VendorDocumentDto {
  id: string;
  type: DocumentType;
  documentUrl: string;
  description?: string;
  status: string;
  uploadedAt: string;
}

export interface VendorPerformanceDto {
  totalProductsSold: number;
  totalRevenue: number;
  currency: string;
  averageRating?: number;
  returnRate: number;
}

export type VendorStatus = 'Draft' | 'PendingApproval' | 'Active' | 'Suspended' | 'Rejected';
export type VendorTier = 'Standard' | 'Silver' | 'Gold' | 'Platinum';
export type DocumentType = 'BusinessLicense' | 'TaxCertificate' | 'IdentityProof' | 'BankStatement' | 'Other';

export interface RegisterVendorRequest {
  businessName: string;
  legalName: string;
  contactEmail: string;
  contactPhone: string;
  street: string;
  city: string;
  country: string;
  postalCode: string;
  taxId?: string;
}

export interface ApproveVendorRequest {
  approvedBy: string;
  notes?: string;
}

export interface RejectVendorRequest {
  rejectedBy: string;
  reason: string;
}

export interface SuspendVendorRequest {
  suspendedBy: string;
  reason: string;
}

export interface AddVendorCategoryRequest {
  categoryId: string;
  categoryName: string;
  customCommissionRate?: number;
}

export interface AddVendorDocumentRequest {
  type: DocumentType;
  documentUrl: string;
  description?: string;
}

export interface RecordSaleRequest {
  productsSold: number;
  revenue: number;
  currency: string;
}

@Injectable({ providedIn: 'root' })
export class VendorsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/vendors`;

  // GET /api/vendors/{id}
  getVendor(id: string): Observable<VendorDto> {
    return this.http.get<VendorDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/vendors/active
  getActiveVendors(skip = 0, take = 50): Observable<VendorDto[]> {
    const params = new HttpParams()
      .set('skip', skip.toString())
      .set('take', take.toString());
    return this.http.get<VendorDto[]>(`${this.baseUrl}/active`, { params });
  }

  // GET /api/vendors/pending-approval
  getPendingApproval(): Observable<VendorDto[]> {
    return this.http.get<VendorDto[]>(`${this.baseUrl}/pending-approval`);
  }

  // GET /api/vendors/tier/{tier}
  getVendorsByTier(tier: VendorTier): Observable<VendorDto[]> {
    return this.http.get<VendorDto[]>(`${this.baseUrl}/tier/${tier}`);
  }

  // GET /api/vendors/category/{categoryId}
  getVendorsByCategory(categoryId: string): Observable<VendorDto[]> {
    return this.http.get<VendorDto[]>(`${this.baseUrl}/category/${categoryId}`);
  }

  // POST /api/vendors - Register vendor
  registerVendor(request: RegisterVendorRequest): Observable<string> {
    return this.http.post<string>(this.baseUrl, request);
  }

  // POST /api/vendors/{id}/submit-for-approval
  submitForApproval(vendorId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${vendorId}/submit-for-approval`, {});
  }

  // POST /api/vendors/{id}/approve
  approve(vendorId: string, request: ApproveVendorRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${vendorId}/approve`, request);
  }

  // POST /api/vendors/{id}/reject
  reject(vendorId: string, request: RejectVendorRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${vendorId}/reject`, request);
  }

  // POST /api/vendors/{id}/suspend
  suspend(vendorId: string, request: SuspendVendorRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${vendorId}/suspend`, request);
  }

  // POST /api/vendors/{vendorId}/categories - Add category
  addCategory(vendorId: string, request: AddVendorCategoryRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${vendorId}/categories`, request);
  }

  // POST /api/vendors/{vendorId}/documents - Add document
  addDocument(vendorId: string, request: AddVendorDocumentRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${vendorId}/documents`, request);
  }

  // POST /api/vendors/{vendorId}/sales - Record sale
  recordSale(vendorId: string, request: RecordSaleRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${vendorId}/sales`, request);
  }
}
