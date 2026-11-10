import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { SupportService, TicketDto, TicketDetailDto, TicketCategory, TicketPriority, CreateTicketRequest } from '@services/support.service';

@Component({
  selector: 'app-support',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './support.component.html',
  styleUrl: './support.component.css'
})
export class SupportComponent implements OnInit {
  private readonly supportService = inject(SupportService);
  
  // State
  tickets = signal<TicketDto[]>([]);
  loading = signal(true);
  activeTab = signal<'all' | 'open' | 'resolved'>('all');
  showCreateModal = signal(false);
  showViewModal = signal(false);
  selectedTicket = signal<TicketDetailDto | null>(null);
  submitting = signal(false);
  sendingReply = signal(false);
  
  // Form data
  newTicket = {
    subject: '',
    description: '',
    category: 'General' as TicketCategory,
    priority: 'Medium' as TicketPriority,
    orderId: ''
  };
  
  replyMessage = '';
  rating = 0;
  ratingFeedback = '';
  
  // Mock customer ID (would come from auth service)
  customerId = 'customer-123';
  customerEmail = 'customer@example.com';
  customerName = 'John Doe';
  
  faqs = [
    { 
      question: 'How do I track my order?', 
      answer: 'You can track your order by going to the Orders section in your account. Click on any order to see its current status and tracking information.',
      open: false 
    },
    { 
      question: 'What is your return policy?', 
      answer: 'We offer a 30-day return policy for most items. Items must be unused and in original packaging. Some exclusions apply for hygiene products and personalized items.',
      open: false 
    },
    { 
      question: 'How long does shipping take?', 
      answer: 'Standard shipping takes 5-7 business days. Express shipping delivers in 2-3 business days. Same-day delivery is available in select areas.',
      open: false 
    },
    { 
      question: 'How do I cancel an order?', 
      answer: 'You can cancel an order within 1 hour of placing it. Go to Orders, find your order, and click Cancel. After 1 hour, please contact support.',
      open: false 
    }
  ];
  
  // Computed
  openTicketsCount = () => this.tickets().filter(t => t.status === 'Open' || t.status === 'InProgress').length;
  resolvedTicketsCount = () => this.tickets().filter(t => t.status === 'Resolved' || t.status === 'Closed').length;
  
  filteredTickets = () => {
    const tab = this.activeTab();
    const all = this.tickets();
    
    if (tab === 'open') {
      return all.filter(t => t.status === 'Open' || t.status === 'InProgress' || t.status === 'WaitingOnCustomer');
    }
    if (tab === 'resolved') {
      return all.filter(t => t.status === 'Resolved' || t.status === 'Closed');
    }
    return all;
  };
  
  ngOnInit() {
    this.loadTickets();
  }
  
  loadTickets() {
    this.loading.set(true);
    this.supportService.getCustomerTickets(this.customerId).subscribe({
      next: (tickets) => {
        this.tickets.set(tickets);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        // Show mock data for demo
        this.tickets.set([
          {
            id: '1',
            ticketNumber: 'TKT-001',
            customerName: 'John Doe',
            subject: 'Order not received',
            category: 'Order',
            priority: 'High',
            status: 'InProgress',
            assignedAgentName: 'Support Agent',
            createdAt: new Date().toISOString()
          },
          {
            id: '2',
            ticketNumber: 'TKT-002',
            customerName: 'John Doe',
            subject: 'Question about product warranty',
            category: 'Product',
            priority: 'Medium',
            status: 'Open',
            createdAt: new Date(Date.now() - 86400000).toISOString()
          }
        ]);
      }
    });
  }
  
  openCreateModal() {
    this.showCreateModal.set(true);
  }
  
  closeCreateModal() {
    this.showCreateModal.set(false);
    this.newTicket = {
      subject: '',
      description: '',
      category: 'General',
      priority: 'Medium',
      orderId: ''
    };
  }
  
  createTicket() {
    if (!this.newTicket.subject || !this.newTicket.description) return;
    
    this.submitting.set(true);
    
    const request: CreateTicketRequest = {
      customerId: this.customerId,
      customerEmail: this.customerEmail,
      customerName: this.customerName,
      subject: this.newTicket.subject,
      description: this.newTicket.description,
      category: this.newTicket.category,
      orderId: this.newTicket.orderId || undefined
    };
    
    this.supportService.createTicket(request).subscribe({
      next: () => {
        this.submitting.set(false);
        this.closeCreateModal();
        this.loadTickets();
      },
      error: () => {
        this.submitting.set(false);
        // For demo, just close modal
        this.closeCreateModal();
      }
    });
  }
  
  viewTicket(ticket: TicketDto) {
    this.supportService.getTicket(ticket.id).subscribe({
      next: (detail) => {
        this.selectedTicket.set(detail);
        this.showViewModal.set(true);
      },
      error: () => {
        // Show mock detail for demo
        this.selectedTicket.set({
          id: ticket.id,
          ticketNumber: ticket.ticketNumber,
          customerId: this.customerId,
          customerEmail: this.customerEmail,
          customerName: this.customerName,
          subject: ticket.subject,
          description: 'This is a detailed description of the issue. I placed an order last week but haven\'t received any shipping updates yet.',
          category: ticket.category,
          channel: 'Web',
          priority: ticket.priority,
          status: ticket.status,
          assignedAgent: ticket.assignedAgentName ? {
            agentId: 'agent-1',
            agentName: ticket.assignedAgentName,
            assignedAt: new Date().toISOString()
          } : undefined,
          messages: [
            {
              id: 'm1',
              senderType: 'Customer',
              content: 'I placed an order last week but haven\'t received any updates about shipping.',
              createdAt: new Date(Date.now() - 86400000).toISOString()
            },
            {
              id: 'm2',
              senderType: 'Agent',
              senderId: 'agent-1',
              senderName: 'Support Agent',
              content: 'Thank you for reaching out. I\'m looking into your order now and will provide an update shortly.',
              createdAt: new Date(Date.now() - 43200000).toISOString()
            }
          ],
          createdAt: ticket.createdAt
        });
        this.showViewModal.set(true);
      }
    });
  }
  
  closeViewModal() {
    this.showViewModal.set(false);
    this.selectedTicket.set(null);
    this.replyMessage = '';
    this.rating = 0;
    this.ratingFeedback = '';
  }
  
  sendReply() {
    if (!this.replyMessage.trim() || !this.selectedTicket()) return;
    
    this.sendingReply.set(true);
    
    this.supportService.addCustomerMessage(this.selectedTicket()!.id, { content: this.replyMessage }).subscribe({
      next: () => {
        this.sendingReply.set(false);
        // Add message locally
        const current = this.selectedTicket()!;
        current.messages.push({
          id: `m${Date.now()}`,
          senderType: 'Customer',
          content: this.replyMessage,
          createdAt: new Date().toISOString()
        });
        this.selectedTicket.set({ ...current });
        this.replyMessage = '';
      },
      error: () => {
        this.sendingReply.set(false);
        // For demo, add message anyway
        const current = this.selectedTicket()!;
        current.messages.push({
          id: `m${Date.now()}`,
          senderType: 'Customer',
          content: this.replyMessage,
          createdAt: new Date().toISOString()
        });
        this.selectedTicket.set({ ...current });
        this.replyMessage = '';
      }
    });
  }
  
  setRating(star: number) {
    this.rating = star;
  }
  
  submitRating() {
    if (this.rating === 0 || !this.selectedTicket()) return;
    
    this.supportService.rateSatisfaction(this.selectedTicket()!.id, {
      rating: this.rating,
      feedback: this.ratingFeedback || undefined
    }).subscribe({
      next: () => {
        const current = this.selectedTicket()!;
        current.satisfactionRating = this.rating;
        current.satisfactionFeedback = this.ratingFeedback;
        this.selectedTicket.set({ ...current });
      },
      error: () => {
        // For demo, update locally
        const current = this.selectedTicket()!;
        current.satisfactionRating = this.rating;
        this.selectedTicket.set({ ...current });
      }
    });
  }
  
  toggleFaq(faq: { open: boolean }) {
    faq.open = !faq.open;
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
  
  formatStatus(status: string): string {
    const map: Record<string, string> = {
      'Open': 'Open',
      'InProgress': 'In Progress',
      'WaitingOnCustomer': 'Waiting on You',
      'Resolved': 'Resolved',
      'Closed': 'Closed'
    };
    return map[status] || status;
  }
  
  getStatusClass(status: string): string {
    const classes: Record<string, string> = {
      'Open': 'bg-yellow-100 text-yellow-800',
      'InProgress': 'bg-blue-100 text-blue-800',
      'WaitingOnCustomer': 'bg-orange-100 text-orange-800',
      'Resolved': 'bg-green-100 text-green-800',
      'Closed': 'bg-gray-100 text-gray-800'
    };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }
  
  getPriorityClass(priority: string): string {
    const classes: Record<string, string> = {
      'Low': 'bg-gray-100 text-gray-800',
      'Medium': 'bg-blue-100 text-blue-800',
      'High': 'bg-orange-100 text-orange-800',
      'Urgent': 'bg-red-100 text-red-800'
    };
    return classes[priority] || 'bg-gray-100 text-gray-800';
  }
}
