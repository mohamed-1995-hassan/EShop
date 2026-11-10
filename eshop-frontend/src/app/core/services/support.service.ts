import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// DTOs
export interface TicketDetailDto {
  id: string;
  ticketNumber: string;
  customerId: string;
  customerEmail: string;
  customerName: string;
  subject: string;
  description: string;
  category: TicketCategory;
  channel: TicketChannel;
  priority: TicketPriority;
  status: TicketStatus;
  orderId?: string;
  assignedAgent?: AssignedAgentDto;
  messages: TicketMessageDto[];
  satisfactionRating?: number;
  satisfactionFeedback?: string;
  createdAt: string;
  firstResponseAt?: string;
  resolvedAt?: string;
  closedAt?: string;
}

export interface TicketDto {
  id: string;
  ticketNumber: string;
  customerName: string;
  subject: string;
  category: TicketCategory;
  priority: TicketPriority;
  status: TicketStatus;
  assignedAgentName?: string;
  createdAt: string;
}

export interface AssignedAgentDto {
  agentId: string;
  agentName: string;
  assignedAt: string;
}

export interface TicketMessageDto {
  id: string;
  senderType: 'Customer' | 'Agent';
  senderId?: string;
  senderName?: string;
  content: string;
  createdAt: string;
}

export type TicketCategory = 'General' | 'Order' | 'Product' | 'Payment' | 'Shipping' | 'Return' | 'Technical' | 'Other';
export type TicketChannel = 'Web' | 'Email' | 'Phone' | 'Chat' | 'Social';
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Urgent';
export type TicketStatus = 'Open' | 'InProgress' | 'WaitingOnCustomer' | 'Resolved' | 'Closed';

export interface CreateTicketRequest {
  customerId: string;
  customerEmail: string;
  customerName: string;
  subject: string;
  description: string;
  category: TicketCategory;
  channel?: TicketChannel;
  orderId?: string;
}

export interface AddMessageRequest {
  content: string;
}

export interface AddAgentMessageRequest {
  agentId: string;
  agentName: string;
  content: string;
}

export interface AssignTicketRequest {
  agentId: string;
  agentName: string;
}

export interface EscalateTicketRequest {
  newPriority: TicketPriority;
  reason: string;
}

export interface ResolveTicketRequest {
  resolution: string;
}

export interface ReopenTicketRequest {
  reason: string;
}

export interface RateSatisfactionRequest {
  rating: number;
  feedback?: string;
}

@Injectable({ providedIn: 'root' })
export class SupportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/support`;

  // GET /api/support/tickets/{id}
  getTicket(id: string): Observable<TicketDetailDto> {
    return this.http.get<TicketDetailDto>(`${this.baseUrl}/tickets/${id}`);
  }

  // GET /api/support/tickets/customer/{customerId}
  getCustomerTickets(customerId: string): Observable<TicketDto[]> {
    return this.http.get<TicketDto[]>(`${this.baseUrl}/tickets/customer/${customerId}`);
  }

  // GET /api/support/tickets/agent/{agentId}
  getAgentTickets(agentId: string): Observable<TicketDto[]> {
    return this.http.get<TicketDto[]>(`${this.baseUrl}/tickets/agent/${agentId}`);
  }

  // GET /api/support/tickets/open
  getOpenTickets(): Observable<TicketDto[]> {
    return this.http.get<TicketDto[]>(`${this.baseUrl}/tickets/open`);
  }

  // GET /api/support/tickets/unassigned
  getUnassignedTickets(): Observable<TicketDto[]> {
    return this.http.get<TicketDto[]>(`${this.baseUrl}/tickets/unassigned`);
  }

  // GET /api/support/tickets/sla-breached
  getSlaBreachedTickets(): Observable<TicketDto[]> {
    return this.http.get<TicketDto[]>(`${this.baseUrl}/tickets/sla-breached`);
  }

  // POST /api/support/tickets - Create ticket
  createTicket(request: CreateTicketRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/tickets`, request);
  }

  // POST /api/support/tickets/{id}/messages/customer - Add customer message
  addCustomerMessage(ticketId: string, request: AddMessageRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/tickets/${ticketId}/messages/customer`, request);
  }

  // POST /api/support/tickets/{id}/messages/agent - Add agent response
  addAgentResponse(ticketId: string, request: AddAgentMessageRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/tickets/${ticketId}/messages/agent`, request);
  }

  // POST /api/support/tickets/{id}/assign - Assign ticket
  assignTicket(ticketId: string, request: AssignTicketRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tickets/${ticketId}/assign`, request);
  }

  // POST /api/support/tickets/{id}/escalate - Escalate ticket
  escalateTicket(ticketId: string, request: EscalateTicketRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tickets/${ticketId}/escalate`, request);
  }

  // POST /api/support/tickets/{id}/resolve - Resolve ticket
  resolveTicket(ticketId: string, request: ResolveTicketRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tickets/${ticketId}/resolve`, request);
  }

  // POST /api/support/tickets/{id}/close - Close ticket
  closeTicket(ticketId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tickets/${ticketId}/close`, {});
  }

  // POST /api/support/tickets/{id}/reopen - Reopen ticket
  reopenTicket(ticketId: string, request: ReopenTicketRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tickets/${ticketId}/reopen`, request);
  }

  // POST /api/support/tickets/{id}/rate - Rate satisfaction
  rateSatisfaction(ticketId: string, request: RateSatisfactionRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tickets/${ticketId}/rate`, request);
  }
}
