import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

// DTOs
export interface AdminUserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: UserRole;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
}

export interface AdminUserDetailDto extends AdminUserDto {
  phone?: string;
  permissions: string[];
  activityLog: UserActivityDto[];
}

export interface UserActivityDto {
  action: string;
  description: string;
  ipAddress?: string;
  createdAt: string;
}

export type UserRole = 'Admin' | 'Manager' | 'Editor' | 'Support' | 'Viewer';

export interface PagedUsersResult {
  items: AdminUserDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  role?: UserRole;
  isActive?: boolean;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  permissions?: string[];
}

export interface CreateUserResult {
  success: boolean;
  userId?: string;
  errorMessage?: string;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  role: UserRole;
  isActive: boolean;
  permissions?: string[];
}

@Injectable({ providedIn: 'root' })
export class AdminUsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin/users`;

  // GET /api/admin/users - Get paginated users
  getUsers(filters: UserFilterParams = {}): Observable<PagedUsersResult> {
    let params = new HttpParams();
    if (filters.page) params = params.set('page', filters.page.toString());
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());
    if (filters.search) params = params.set('search', filters.search);
    if (filters.role) params = params.set('role', filters.role);
    if (filters.isActive !== undefined) params = params.set('isActive', filters.isActive.toString());

    return this.http.get<PagedUsersResult>(this.baseUrl, { params });
  }

  // GET /api/admin/users/{id} - Get user details
  getUser(id: string): Observable<AdminUserDetailDto> {
    return this.http.get<AdminUserDetailDto>(`${this.baseUrl}/${id}`);
  }

  // POST /api/admin/users - Create user
  createUser(request: CreateUserRequest): Observable<CreateUserResult> {
    return this.http.post<CreateUserResult>(this.baseUrl, request);
  }

  // PUT /api/admin/users/{id} - Update user
  updateUser(id: string, request: UpdateUserRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  // DELETE /api/admin/users/{id} - Delete user
  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // POST /api/admin/users/{id}/reset-password - Reset password
  resetPassword(id: string): Observable<{ temporaryPassword: string }> {
    return this.http.post<{ temporaryPassword: string }>(`${this.baseUrl}/${id}/reset-password`, {});
  }
}
