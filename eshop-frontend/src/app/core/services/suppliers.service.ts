import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface SupplierDto {
  id: string;
  code: string;
  name: string;
  contactName: string;
  email: string;
  currency: string;
  status: SupplierStatus;
  products: SupplierProductDto[];
  performance: SupplierPerformanceDto;
  createdAt: string;
  approvedAt?: string;
}

export interface SupplierSummaryDto {
  id: string;
  code: string;
  name: string;
  status: SupplierStatus;
  productCount: number;
  onTimeDeliveryRate: number;
}

export interface SupplierProductDto {
  id: string;
  productId: string;
  sku: string;
  name: string;
  unitCost: number;
  currency: string;
  minOrderQty: number;
  leadTimeDays?: number;
}

export interface SupplierPerformanceDto {
  totalDeliveries: number;
  onTimeDeliveries: number;
  lateDeliveries: number;
  qualityIssues: number;
  onTimeDeliveryRate: number;
  qualityScore: number;
}

export type SupplierStatus = 'Draft' | 'Active' | 'Suspended';

// Purchase Order DTOs
export interface PurchaseOrderDto {
  id: string;
  poNumber: string;
  supplierId: string;
  supplierName: string;
  status: POStatus;
  lines: PurchaseOrderLineDto[];
  totalAmount: number;
  currency: string;
  warehouseId?: string;
  createdBy: string;
  approvedBy?: string;
  createdAt: string;
  approvedAt?: string;
  sentAt?: string;
}

export interface PurchaseOrderLineDto {
  id: string;
  productId: string;
  productName: string;
  sku: string;
  quantity: number;
  receivedQuantity: number;
  unitCost: number;
  currency: string;
  totalCost: number;
}

export type POStatus = 'Draft' | 'Submitted' | 'Approved' | 'Sent' | 'PartiallyReceived' | 'Received' | 'Cancelled';

// Request DTOs
export interface CreateSupplierRequest {
  code: string;
  name: string;
  contactName: string;
  email: string;
  currency?: string;
}

export interface SuspendSupplierRequest {
  reason: string;
}

export interface AddSupplierProductRequest {
  productId: string;
  sku: string;
  name: string;
  unitCost: number;
  currency: string;
  minOrderQty?: number;
  leadTimeDays?: number;
}

export interface UpdateCostRequest {
  newCost: number;
  currency: string;
}

export interface RecordDeliveryRequest {
  onTime: boolean;
  hasQualityIssue: boolean;
}

export interface CreatePurchaseOrderRequest {
  supplierId: string;
  supplierName: string;
  createdBy: string;
  currency: string;
  warehouseId?: string;
}

export interface AddPurchaseOrderLineRequest {
  productId: string;
  productName: string;
  sku: string;
  quantity: number;
  unitCost: number;
  currency: string;
}

export interface ApprovePurchaseOrderRequest {
  approvedBy: string;
}

export interface ReceivePurchaseOrderRequest {
  receivedBy: string;
  quantitiesReceived: { [lineId: string]: number };
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class SuppliersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/suppliers`;

  // === SUPPLIERS ===

  // GET /api/suppliers/{id}
  getSupplier(id: string): Observable<SupplierDto> {
    return this.http.get<SupplierDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/suppliers/active
  getActiveSuppliers(): Observable<SupplierSummaryDto[]> {
    return this.http.get<SupplierSummaryDto[]>(`${this.baseUrl}/active`);
  }

  // GET /api/suppliers/product/{productId}
  getSuppliersForProduct(productId: string): Observable<SupplierSummaryDto[]> {
    return this.http.get<SupplierSummaryDto[]>(`${this.baseUrl}/product/${productId}`);
  }

  // POST /api/suppliers - Create supplier
  createSupplier(request: CreateSupplierRequest): Observable<string> {
    return this.http.post<string>(this.baseUrl, request);
  }

  // POST /api/suppliers/{id}/approve
  approveSupplier(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/approve`, {});
  }

  // POST /api/suppliers/{id}/suspend
  suspendSupplier(id: string, request: SuspendSupplierRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/suspend`, request);
  }

  // POST /api/suppliers/{id}/products - Add product
  addProduct(supplierId: string, request: AddSupplierProductRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${supplierId}/products`, request);
  }

  // PUT /api/suppliers/{id}/products/{productId}/cost - Update cost
  updateProductCost(supplierId: string, productId: string, request: UpdateCostRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${supplierId}/products/${productId}/cost`, request);
  }

  // POST /api/suppliers/{id}/delivery - Record delivery
  recordDelivery(supplierId: string, request: RecordDeliveryRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${supplierId}/delivery`, request);
  }

  // === PURCHASE ORDERS ===

  // GET /api/suppliers/purchase-orders/{id}
  getPurchaseOrder(id: string): Observable<PurchaseOrderDto> {
    return this.http.get<PurchaseOrderDto>(`${this.baseUrl}/purchase-orders/${id}`);
  }

  // GET /api/suppliers/purchase-orders/pending-approval
  getPendingApprovalPOs(): Observable<PurchaseOrderDto[]> {
    return this.http.get<PurchaseOrderDto[]>(`${this.baseUrl}/purchase-orders/pending-approval`);
  }

  // GET /api/suppliers/purchase-orders/awaiting-delivery
  getAwaitingDeliveryPOs(): Observable<PurchaseOrderDto[]> {
    return this.http.get<PurchaseOrderDto[]>(`${this.baseUrl}/purchase-orders/awaiting-delivery`);
  }

  // POST /api/suppliers/purchase-orders - Create PO
  createPurchaseOrder(request: CreatePurchaseOrderRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/purchase-orders`, request);
  }

  // POST /api/suppliers/purchase-orders/{id}/lines - Add line
  addPurchaseOrderLine(poId: string, request: AddPurchaseOrderLineRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/purchase-orders/${poId}/lines`, request);
  }

  // POST /api/suppliers/purchase-orders/{id}/submit
  submitPurchaseOrder(poId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/purchase-orders/${poId}/submit`, {});
  }

  // POST /api/suppliers/purchase-orders/{id}/approve
  approvePurchaseOrder(poId: string, request: ApprovePurchaseOrderRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/purchase-orders/${poId}/approve`, request);
  }

  // POST /api/suppliers/purchase-orders/{id}/send
  sendPurchaseOrder(poId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/purchase-orders/${poId}/send`, {});
  }

  // POST /api/suppliers/purchase-orders/{id}/receive
  receivePurchaseOrder(poId: string, request: ReceivePurchaseOrderRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/purchase-orders/${poId}/receive`, request);
  }
}
