import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface ShipmentDto {
  id: string;
  orderId: string;
  orderNumber: string;
  trackingNumber?: string;
  carrier?: string;
  status: ShipmentStatus;
  estimatedDelivery?: string;
  trackingEvents: TrackingEventDto[];
  shippingAddress: ShipmentAddressDto;
  weight?: number;
  dimensions?: ShipmentDimensionsDto;
  createdAt: string;
  shippedAt?: string;
  deliveredAt?: string;
}

export interface TrackingEventDto {
  status: string;
  description: string;
  location?: string;
  timestamp: string;
}

export interface ShipmentAddressDto {
  street: string;
  city: string;
  country: string;
  postalCode: string;
}

export interface ShipmentDimensionsDto {
  length: number;
  width: number;
  height: number;
  unit: string;
}

export type ShipmentStatus = 'Created' | 'ReadyForPickup' | 'PickedUp' | 'InTransit' | 'OutForDelivery' | 'Delivered' | 'DeliveryAttemptFailed' | 'Returned';

export interface CreateShipmentRequest {
  orderId: string;
  carrier: string;
  shippingMethod: string;
  street: string;
  city: string;
  country: string;
  postalCode: string;
  weight?: number;
  length?: number;
  width?: number;
  height?: number;
}

export interface CreateShipmentResult {
  success: boolean;
  shipmentId?: string;
  errorMessage?: string;
}

export interface SetTrackingRequest {
  trackingNumber: string;
  carrier: string;
  estimatedDelivery?: string;
}

export interface UpdateLocationRequest {
  location: string;
  description: string;
}

export interface RecordDeliveryAttemptRequest {
  reason: string;
  rescheduledDate?: string;
}

@Injectable({ providedIn: 'root' })
export class ShipmentsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/shipments`;

  // GET /api/shipments/{id}
  getShipment(id: string): Observable<ShipmentDto> {
    return this.http.get<ShipmentDto>(`${this.baseUrl}/${id}`);
  }

  // GET /api/shipments/track/{trackingNumber}
  trackShipment(trackingNumber: string): Observable<ShipmentDto> {
    return this.http.get<ShipmentDto>(`${this.baseUrl}/track/${trackingNumber}`);
  }

  // POST /api/shipments - Create shipment
  createShipment(request: CreateShipmentRequest): Observable<CreateShipmentResult> {
    return this.http.post<CreateShipmentResult>(this.baseUrl, request);
  }

  // POST /api/shipments/{id}/tracking - Set tracking info
  setTracking(shipmentId: string, request: SetTrackingRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${shipmentId}/tracking`, request);
  }

  // POST /api/shipments/{id}/ready-for-pickup - Mark ready for pickup
  markReadyForPickup(shipmentId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${shipmentId}/ready-for-pickup`, {});
  }

  // POST /api/shipments/{id}/pickup - Mark picked up
  markPickedUp(shipmentId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${shipmentId}/pickup`, {});
  }

  // POST /api/shipments/{id}/location - Update location
  updateLocation(shipmentId: string, request: UpdateLocationRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${shipmentId}/location`, request);
  }

  // POST /api/shipments/{id}/out-for-delivery - Mark out for delivery
  markOutForDelivery(shipmentId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${shipmentId}/out-for-delivery`, {});
  }

  // POST /api/shipments/{id}/deliver - Mark delivered
  markDelivered(shipmentId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${shipmentId}/deliver`, {});
  }

  // POST /api/shipments/{id}/delivery-attempt - Record delivery attempt
  recordDeliveryAttempt(shipmentId: string, request: RecordDeliveryAttemptRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${shipmentId}/delivery-attempt`, request);
  }

  // GET /api/shipments/order/{orderId}
  getShipmentsByOrder(orderId: string): Observable<ShipmentDto[]> {
    return this.http.get<ShipmentDto[]>(`${this.baseUrl}/order/${orderId}`);
  }

  // GET /api/shipments/pending
  getPendingShipments(): Observable<ShipmentDto[]> {
    return this.http.get<ShipmentDto[]>(`${this.baseUrl}/pending`);
  }
}
