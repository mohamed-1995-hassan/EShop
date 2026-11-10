import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';

interface AdminUser {
  id: string;
  name: string;
  email: string;
  role: 'Admin' | 'Manager' | 'Editor' | 'Support';
  status: 'Active' | 'Inactive' | 'Suspended';
  lastLogin?: string;
  createdAt: string;
}

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css'
})
export class AdminUsersComponent implements OnInit {
  users = signal<AdminUser[]>([]);
  showModal = signal(false);
  editingUser = signal<AdminUser | null>(null);
  searchQuery = '';
  roleFilter = '';
  
  formData = { name: '', email: '', role: 'Editor' as AdminUser['role'], password: '' };
  
  activeCount = () => this.users().filter(u => u.status === 'Active').length;
  adminCount = () => this.users().filter(u => u.role === 'Admin').length;
  recentLogins = () => this.users().filter(u => u.lastLogin && new Date(u.lastLogin).toDateString() === new Date().toDateString()).length;
  
  filteredUsers = () => {
    let result = this.users();
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(u => u.name.toLowerCase().includes(q) || u.email.toLowerCase().includes(q));
    }
    if (this.roleFilter) {
      result = result.filter(u => u.role === this.roleFilter);
    }
    return result;
  };
  
  ngOnInit() {
    this.users.set([
      { id: '1', name: 'John Admin', email: 'john@eshop.com', role: 'Admin', status: 'Active', lastLogin: new Date().toISOString(), createdAt: new Date(Date.now() - 31536000000).toISOString() },
      { id: '2', name: 'Sarah Manager', email: 'sarah@eshop.com', role: 'Manager', status: 'Active', lastLogin: new Date(Date.now() - 86400000).toISOString(), createdAt: new Date(Date.now() - 15768000000).toISOString() },
      { id: '3', name: 'Mike Editor', email: 'mike@eshop.com', role: 'Editor', status: 'Active', lastLogin: new Date(Date.now() - 172800000).toISOString(), createdAt: new Date(Date.now() - 7884000000).toISOString() },
      { id: '4', name: 'Lisa Support', email: 'lisa@eshop.com', role: 'Support', status: 'Suspended', createdAt: new Date(Date.now() - 5256000000).toISOString() },
      { id: '5', name: 'Tom Editor', email: 'tom@eshop.com', role: 'Editor', status: 'Active', lastLogin: new Date().toISOString(), createdAt: new Date(Date.now() - 2628000000).toISOString() }
    ]);
  }
  
  openAddModal() {
    this.editingUser.set(null);
    this.formData = { name: '', email: '', role: 'Editor', password: '' };
    this.showModal.set(true);
  }
  
  editUser(user: AdminUser) {
    this.editingUser.set(user);
    this.formData = { name: user.name, email: user.email, role: user.role, password: '' };
    this.showModal.set(true);
  }
  
  closeModal() {
    this.showModal.set(false);
    this.editingUser.set(null);
  }
  
  saveUser() {
    if (this.editingUser()) {
      const users = this.users();
      const idx = users.findIndex(u => u.id === this.editingUser()!.id);
      if (idx >= 0) {
        users[idx] = { ...users[idx], name: this.formData.name, email: this.formData.email, role: this.formData.role };
        this.users.set([...users]);
      }
    } else {
      const newUser: AdminUser = {
        id: Date.now().toString(),
        name: this.formData.name,
        email: this.formData.email,
        role: this.formData.role,
        status: 'Active',
        createdAt: new Date().toISOString()
      };
      this.users.set([newUser, ...this.users()]);
    }
    this.closeModal();
  }
  
  suspendUser(user: AdminUser) {
    const users = this.users();
    const idx = users.findIndex(u => u.id === user.id);
    if (idx >= 0) {
      users[idx] = { ...users[idx], status: 'Suspended' };
      this.users.set([...users]);
    }
  }
  
  activateUser(user: AdminUser) {
    const users = this.users();
    const idx = users.findIndex(u => u.id === user.id);
    if (idx >= 0) {
      users[idx] = { ...users[idx], status: 'Active' };
      this.users.set([...users]);
    }
  }
  
  deleteUser(user: AdminUser) {
    if (confirm(`Delete user ${user.name}?`)) {
      this.users.set(this.users().filter(u => u.id !== user.id));
    }
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleString('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
  
  getRoleClass(role: string): string {
    const classes: Record<string, string> = {
      'Admin': 'bg-purple-100 text-purple-800',
      'Manager': 'bg-blue-100 text-blue-800',
      'Editor': 'bg-green-100 text-green-800',
      'Support': 'bg-yellow-100 text-yellow-800'
    };
    return classes[role] || 'bg-gray-100 text-gray-800';
  }
  
  getStatusClass(status: string): string {
    const classes: Record<string, string> = {
      'Active': 'bg-green-100 text-green-800',
      'Inactive': 'bg-gray-100 text-gray-800',
      'Suspended': 'bg-red-100 text-red-800'
    };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }
}
