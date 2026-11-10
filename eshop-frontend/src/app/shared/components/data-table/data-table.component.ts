import { Component, input, output, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';
  type?: 'text' | 'number' | 'date' | 'currency' | 'badge' | 'image' | 'actions';
  badgeColors?: Record<string, string>;
  format?: (value: any, row: any) => string;
}

export interface TableAction {
  icon: string;
  label: string;
  action: string;
  color?: string;
  show?: (row: any) => boolean;
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.css'
})
export class DataTableComponent<T = any> {
  // Inputs
  columns = input.required<TableColumn[]>();
  data = input.required<T[]>();
  actions = input<TableAction[]>([]);
  loading = input<boolean>(false);
  selectable = input<boolean>(false);
  searchable = input<boolean>(true);
  showToolbar = input<boolean>(true);
  showPagination = input<boolean>(true);
  emptyMessage = input<string>('No data found');
  trackByFn = input<(row: T) => any>((row: any) => row.id);
  
  // Pagination inputs
  totalItems = input<number>(0);
  currentPage = input<number>(1);
  pageSize = input<number>(10);
  
  // Outputs
  search = output<string>();
  sort = output<{ sortBy: string; sortOrder: 'asc' | 'desc' }>();
  pageChange = output<number>();
  selectionChange = output<T[]>();
  actionClick = output<{ action: string; row: T }>();
  
  // Internal state
  searchQuery = signal('');
  sortBy = signal('');
  sortOrder = signal<'asc' | 'desc'>('asc');
  selectedItems = signal<Set<any>>(new Set());
  
  // Computed
  totalPages = computed(() => Math.ceil(this.totalItems() / this.pageSize()));
  startItem = computed(() => (this.currentPage() - 1) * this.pageSize() + 1);
  endItem = computed(() => Math.min(this.currentPage() * this.pageSize(), this.totalItems()));
  
  allSelected = computed(() => {
    const data = this.data();
    return data.length > 0 && this.selectedItems().size === data.length;
  });
  
  someSelected = computed(() => {
    const selected = this.selectedItems().size;
    return selected > 0 && selected < this.data().length;
  });
  
  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: (number | string)[] = [];
    
    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push('...');
      
      for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
        pages.push(i);
      }
      
      if (current < total - 2) pages.push('...');
      pages.push(total);
    }
    
    return pages;
  });
  
  // Methods
  getValue(row: any, key: string): any {
    return key.split('.').reduce((obj, k) => obj?.[k], row);
  }
  
  getBadgeColor(value: string, colors?: Record<string, string>): string {
    const defaultColors: Record<string, string> = {
      'Active': '#10b981',
      'Inactive': '#6b7280',
      'Pending': '#f59e0b',
      'Completed': '#10b981',
      'Cancelled': '#ef4444',
      'Processing': '#3b82f6',
      'Shipped': '#8b5cf6',
      'Delivered': '#10b981',
      'Failed': '#ef4444',
      'Paid': '#10b981',
      'Refunded': '#f59e0b'
    };
    return colors?.[value] ?? defaultColors[value] ?? '#6b7280';
  }
  
  onSearch(query: string) {
    this.searchQuery.set(query);
    this.search.emit(query);
  }
  
  onSort(key: string) {
    if (this.sortBy() === key) {
      this.sortOrder.update(o => o === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(key);
      this.sortOrder.set('asc');
    }
    this.sort.emit({ sortBy: this.sortBy(), sortOrder: this.sortOrder() });
  }
  
  onPageChange(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.pageChange.emit(page);
    }
  }
  
  toggleSelect(row: T) {
    const trackFn = this.trackByFn();
    const id = trackFn(row);
    this.selectedItems.update(set => {
      const newSet = new Set(set);
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
    this.emitSelection();
  }
  
  toggleSelectAll(event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    const trackFn = this.trackByFn();
    
    if (checked) {
      this.selectedItems.set(new Set(this.data().map(row => trackFn(row))));
    } else {
      this.selectedItems.set(new Set());
    }
    this.emitSelection();
  }
  
  isSelected(row: T): boolean {
    const trackFn = this.trackByFn();
    return this.selectedItems().has(trackFn(row));
  }
  
  private emitSelection() {
    const trackFn = this.trackByFn();
    const selected = this.data().filter(row => this.selectedItems().has(trackFn(row)));
    this.selectionChange.emit(selected);
  }
  
  onAction(action: string, row: T) {
    this.actionClick.emit({ action, row });
  }
}
