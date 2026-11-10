import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface ReturnRequestDto {
  id: string;
  returnNumber: string;
  orderId: string;
  orderNumber: string;
  customerId: string;
  status: ReturnStatus;
  items: ReturnItemDto[];
  totalRefundAmount: number;
  currency: string;
  shippingLabel?: ShippingLabelDto;
  images: string[];
  createdAt: string;
  submittedAt?: string;
  receivedAt?: string;
  completedAt?: string;
}

export interface ReturnItemDto {
  id: string;
  orderItemId: string;
  productId: string;
  productName: string;
  quantity: number;
  reason: string;
  condition?: ItemCondition;
  refundAmount: number;
}

export interface ShippingLabelDto {
  trackingNumber: string;
  carrier: string;
  labelUrl: string;
}

export type ReturnStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected' | 'Shipped' | 'Received' | 'Inspecting' | 'Completed' | 'Cancelled';
export type ItemCondition = 'New' | 'Opened' | 'Used' | 'Damaged' | 'Defective';

export interface CreateReturnRequest {
  orderId: string;
  customerId: string;
  customerEmail: string;
  customerName: string;
}

export interface CreateReturnResult {
  success: boolean;
  returnId?: string;
  returnNumber?: string;
  errorMessage?: string;
}

export interface AddReturnItemRequest {
  orderItemId: string;
  productId: string;
  productName: string;
  quantity: number;
  reason: string;
  unitRefundAmount: number;
  currency: string;
}

export interface AddReturnImageRequest {
  imageUrl: string;
  description?: string;
}

export interface ApproveReturnRequest {
  approvedBy: string;
  notes?: string;
}

export interface RejectReturnRequest {
  rejectedBy: string;
  reason: string;
}

export interface GenerateLabelRequest {
  carrier: string;
  weight: number;
}

export interface GenerateLabelResult {
  trackingNumber: string;
  labelUrl: string;
}

export interface ReceiveReturnRequest {
  receivedBy: string;
  notes?: string;
}

export interface InspectItemRequest {
  condition: ItemCondition;
  notes?: string;
  refundApproved: boolean;
  adjustedRefundAmount?: number;
}

export interface ProcessRefundRequest {
  refundMethod: 'Original' | 'StoreCredit' | 'GiftCard';
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class ReturnsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/returns`;

  // GET /api/returns/{id}
  getReturn(id: string): Observable<ReturnRequestDto> {
    return this.http.get<ReturnRequestDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/returns/number/{returnNumber}
  getReturnByNumber(returnNumber: string): Observable<ReturnRequestDto> {
    return this.http.get<ReturnRequestDto>(`${this.baseUrl}/number/${returnNumber}`);
  }

  // POST /api/returns - Create return request
  createReturn(request: CreateReturnRequest): Observable<CreateReturnResult> {
    return this.http.post<CreateReturnResult>(this.baseUrl, request);
  }

  // POST /api/returns/{id}/items - Add item to return
  addItem(returnId: string, request: AddReturnItemRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${returnId}/items`, request);
  }

  // POST /api/returns/{id}/images - Add image
  addImage(returnId: string, request: AddReturnImageRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${returnId}/images`, request);
  }

  // POST /api/returns/{id}/submit - Submit return request
  submit(returnId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${returnId}/submit`, {});
  }

  // POST /api/returns/{id}/approve - Approve return
  approve(returnId: string, request: ApproveReturnRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${returnId}/approve`, request);
  }

  // POST /api/returns/{id}/reject - Reject return
  reject(returnId: string, request: RejectReturnRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${returnId}/reject`, request);
  }

  // POST /api/returns/{id}/label - Generate shipping label
  generateLabel(returnId: string, request: GenerateLabelRequest): Observable<GenerateLabelResult> {
    return this.http.post<GenerateLabelResult>(`${this.baseUrl}/${returnId}/label`, request);
  }

  // POST /api/returns/{id}/ship - Mark as shipped
  markShipped(returnId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${returnId}/ship`, {});
  }

  // POST /api/returns/{id}/receive - Mark as received
  markReceived(returnId: string, request: ReceiveReturnRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${returnId}/receive`, request);
  }

  // POST /api/returns/{id}/inspect/start - Start inspection
  startInspection(returnId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${returnId}/inspect/start`, {});
  }

  // POST /api/returns/{id}/inspect/{itemId} - Inspect item
  inspectItem(returnId: string, itemId: string, request: InspectItemRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${returnId}/inspect/${itemId}`, request);
  }

  // POST /api/returns/{id}/inspect/complete - Complete inspection
  completeInspection(returnId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${returnId}/inspect/complete`, {});
  }

  // POST /api/returns/{id}/refund - Process refund
  processRefund(returnId: string, request: ProcessRefundRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${returnId}/refund`, request);
  }

  // GET /api/returns/customer/{customerId}
  getCustomerReturns(customerId: string): Observable<ReturnRequestDto[]> {
    return this.http.get<ReturnRequestDto[]>(`${this.baseUrl}/customer/${customerId}`);
  }

  // GET /api/returns/pending-approval
  getPendingApproval(): Observable<ReturnRequestDto[]> {
    return this.http.get<ReturnRequestDto[]>(`${this.baseUrl}/pending-approval`);
  }

  // GET /api/returns/awaiting-inspection
  getAwaitingInspection(): Observable<ReturnRequestDto[]> {
    return this.http.get<ReturnRequestDto[]>(`${this.baseUrl}/awaiting-inspection`);
  }
}
