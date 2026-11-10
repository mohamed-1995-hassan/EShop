import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { CustomersService, CustomerProfileDto } from '@core/services/customers.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-admin-customers',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './admin-customers.component.html',
  styleUrl: './admin-customers.component.css'
})
export class AdminCustomersComponent implements OnInit {
  private customersService = inject(CustomersService);
  
  sidebarOpen = signal(true);
  customers = signal<CustomerProfileDto[]>([]);
  loading = signal(true);
  searchTerm = '';
  
  ngOnInit() {
    this.loadCustomers();
  }
  
  loadCustomers() {
    this.loading.set(true);
    // Note: In a real app, you'd have an admin endpoint to list all customers
    // For now, we'll show empty state
    this.loading.set(false);
  }
  
  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
  
  creditWallet(customer: CustomerProfileDto) {
    const amount = prompt('Enter amount to credit:');
    if (amount && !isNaN(Number(amount))) {
      this.customersService.creditWallet(customer.id, {
        amount: Number(amount),
        description: 'Admin credit',
        type: 'Credit'
      }).subscribe({
        next: (result) => {
          if (result.success) {
            alert(`Wallet credited. New balance: ${result.newBalance}`);
            this.loadCustomers();
          } else {
            alert(result.errorMessage || 'Failed to credit wallet');
          }
        },
        error: (err) => console.error('Error crediting wallet:', err)
      });
    }
  }
}
