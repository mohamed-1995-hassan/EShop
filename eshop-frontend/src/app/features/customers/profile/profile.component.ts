import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { CustomersService, CustomerProfileDto } from '@core/services/customers.service';
import { AuthService } from '@core/auth/auth.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule, HeaderComponent, FooterComponent, LoadingComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private customersService = inject(CustomersService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  
  profile = signal<CustomerProfileDto | null>(null);
  loading = signal(true);
  saving = signal(false);
  
  profileForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    dateOfBirth: ['']
  });
  
  ngOnInit() {
    this.loadProfile();
  }
  
  loadProfile() {
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
        this.profile.set(profile);
        this.profileForm.patchValue({
          firstName: profile.firstName,
          lastName: profile.lastName,
          email: profile.email,
          phone: profile.phone || '',
          dateOfBirth: profile.dateOfBirth || ''
        });
      }
      this.loading.set(false);
    });
  }
  
  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
  
  updateProfile() {
    if (this.profileForm.invalid) return;
    this.saving.set(true);
    // API call would go here
    setTimeout(() => {
      this.saving.set(false);
    }, 1000);
  }
  
  logout() {
    this.authService.logout();
  }
}
