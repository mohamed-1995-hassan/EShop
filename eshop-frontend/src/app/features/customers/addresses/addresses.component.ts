import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { CustomersService, CustomerProfileDto, CustomerAddressDto } from '@core/services/customers.service';
import { AuthService } from '@core/auth/auth.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-addresses',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule, HeaderComponent, FooterComponent, LoadingComponent],
  templateUrl: './addresses.component.html',
  styleUrl: './addresses.component.css'
})
export class AddressesComponent implements OnInit {
  private customersService = inject(CustomersService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  
  addresses = signal<CustomerAddressDto[]>([]);
  loading = signal(true);
  saving = signal(false);
  showAddModal = signal(false);
  editingAddress = signal<CustomerAddressDto | null>(null);
  
  addressForm = this.fb.group({
    label: ['', Validators.required],
    street: ['', Validators.required],
    city: ['', Validators.required],
    postalCode: ['', Validators.required],
    country: ['', Validators.required],
    isDefault: [false]
  });
  
  ngOnInit() {
    this.loadAddresses();
  }
  
  loadAddresses() {
    const userId = this.authService.currentUser()?.id;
    if (!userId) {
      this.loading.set(false);
      return;
    }
    
    this.customersService.getProfile(userId).pipe(
      catchError(err => {
        console.error('Error loading profile:', err);
        return of(null);
      })
    ).subscribe(profile => {
      if (profile) {
        this.addresses.set(profile.addresses || []);
      }
      this.loading.set(false);
    });
  }
  
  editAddress(address: CustomerAddressDto) {
    this.editingAddress.set(address);
    this.addressForm.patchValue(address);
    this.showAddModal.set(true);
  }
  
  closeModal() {
    this.showAddModal.set(false);
    this.editingAddress.set(null);
    this.addressForm.reset();
  }
  
  saveAddress() {
    if (this.addressForm.invalid) return;
    
    this.saving.set(true);
    const userId = this.authService.currentUser()?.id;
    if (!userId) return;
    
    this.customersService.addAddress(userId, this.addressForm.value as any).subscribe({
      next: () => {
        this.closeModal();
        this.loadAddresses();
        this.saving.set(false);
      },
      error: (err) => {
        console.error('Error saving address:', err);
        this.saving.set(false);
      }
    });
  }
  
  deleteAddress(addressId: string) {
    if (confirm('Are you sure you want to delete this address?')) {
      // API call would go here
      this.addresses.update(addrs => addrs.filter(a => a.id !== addressId));
    }
  }
  
  setDefault(addressId: string) {
    this.addresses.update(addrs => 
      addrs.map(a => ({ ...a, isDefault: a.id === addressId }))
    );
  }
}
