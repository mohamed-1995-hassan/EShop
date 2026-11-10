import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '@env/environment';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  avatar?: string;
}

// Request DTOs matching backend
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  dateOfBirth?: string;
  phone?: string;
  acceptTerms: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ExternalLoginRequest {
  provider: string;
  idToken: string;
  email?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface ConfirmEmailRequest {
  userId: string;
  token: string;
}

// Response DTOs matching backend
export interface AuthResponse {
  success: boolean;
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: User;
  errorMessage?: string;
}

export interface ForgotPasswordResponse {
  success: boolean;
  message: string;
}

export interface CurrentUserResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isEmailConfirmed: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private baseUrl = `${environment.apiUrl}/auth`;
  
  // Signals for reactive state
  private currentUser = signal<User | null>(null);
  private isAuthenticated = signal<boolean>(false);
  
  // Computed values
  readonly user = computed(() => this.currentUser());
  readonly loggedIn = computed(() => this.isAuthenticated());
  readonly isAdmin = computed(() => this.currentUser()?.roles.includes('Admin') ?? false);
  readonly isVendor = computed(() => this.currentUser()?.roles.includes('Vendor') ?? false);
  
  constructor() {
    this.loadStoredAuth();
  }
  
  // POST /api/auth/register - Register new user
  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, data).pipe(
      tap(response => {
        if (response.success) {
          this.handleAuthSuccess(response);
        }
      }),
      catchError(error => this.handleAuthError(error))
    );
  }
  
  // POST /api/auth/login - Login
  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, credentials).pipe(
      tap(response => {
        if (response.success) {
          this.handleAuthSuccess(response);
        }
      }),
      catchError(error => this.handleAuthError(error))
    );
  }
  
  // POST /api/auth/refresh - Refresh access token
  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem('refreshToken');
    const request: RefreshTokenRequest = { refreshToken: refreshToken || '' };
    return this.http.post<AuthResponse>(`${this.baseUrl}/refresh`, request).pipe(
      tap(response => {
        if (response.success) {
          this.handleAuthSuccess(response);
        }
      }),
      catchError(error => {
        this.logout();
        return throwError(() => error);
      })
    );
  }
  
  // POST /api/auth/external-login - OAuth login (Google, Facebook, etc.)
  externalLogin(request: ExternalLoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/external-login`, request).pipe(
      tap(response => {
        if (response.success) {
          this.handleAuthSuccess(response);
        }
      }),
      catchError(error => this.handleAuthError(error))
    );
  }
  
  // POST /api/auth/logout - Logout
  logout(): void {
    const refreshToken = localStorage.getItem('refreshToken');
    
    // Call logout endpoint (fire and forget)
    if (refreshToken) {
      this.http.post(`${this.baseUrl}/logout`, { refreshToken }).subscribe();
    }
    
    // Clear local state
    this.clearAuth();
    this.router.navigate(['/auth/login']);
  }
  
  // POST /api/auth/forgot-password - Request password reset
  forgotPassword(email: string): Observable<ForgotPasswordResponse> {
    const request: ForgotPasswordRequest = { email };
    return this.http.post<ForgotPasswordResponse>(`${this.baseUrl}/forgot-password`, request);
  }
  
  // POST /api/auth/reset-password - Reset password with token
  resetPassword(email: string, token: string, newPassword: string): Observable<{ success: boolean }> {
    const request: ResetPasswordRequest = { email, token, newPassword };
    return this.http.post<{ success: boolean }>(`${this.baseUrl}/reset-password`, request);
  }
  
  // POST /api/auth/confirm-email - Confirm email address
  confirmEmail(userId: string, token: string): Observable<{ success: boolean }> {
    const request: ConfirmEmailRequest = { userId, token };
    return this.http.post<{ success: boolean }>(`${this.baseUrl}/confirm-email`, request);
  }
  
  // GET /api/auth/me - Get current user info
  getCurrentUser(): Observable<CurrentUserResponse> {
    return this.http.get<CurrentUserResponse>(`${this.baseUrl}/me`).pipe(
      tap(user => {
        this.currentUser.set({
          id: user.id,
          email: user.email,
          firstName: user.firstName,
          lastName: user.lastName,
          roles: user.roles
        });
      })
    );
  }
  
  // Get access token
  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }
  
  // Check if token is expired
  isTokenExpired(): boolean {
    const expiry = localStorage.getItem('tokenExpiry');
    if (!expiry) return true;
    return Date.now() > parseInt(expiry, 10);
  }
  
  // Handle successful authentication
  private handleAuthSuccess(response: AuthResponse): void {
    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('tokenExpiry', (Date.now() + response.expiresIn * 1000).toString());
    localStorage.setItem('user', JSON.stringify(response.user));
    
    this.currentUser.set(response.user);
    this.isAuthenticated.set(true);
  }
  
  // Handle auth error
  private handleAuthError(error: any): Observable<never> {
    console.error('Auth error:', error);
    return throwError(() => error);
  }
  
  // Clear auth data
  private clearAuth(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('tokenExpiry');
    localStorage.removeItem('user');
    
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
  }
  
  // Load stored auth on app init
  private loadStoredAuth(): void {
    const token = localStorage.getItem('accessToken');
    const userJson = localStorage.getItem('user');
    
    if (token && userJson && !this.isTokenExpired()) {
      try {
        const user = JSON.parse(userJson);
        this.currentUser.set(user);
        this.isAuthenticated.set(true);
      } catch {
        this.clearAuth();
      }
    } else {
      this.clearAuth();
    }
  }
}
