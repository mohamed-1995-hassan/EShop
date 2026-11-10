import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';

interface Page { id: string; title: string; slug: string; content: string; isPublished: boolean; updatedAt: string; }

@Component({
  selector: 'app-pages',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './pages.component.html',
  styleUrl: './pages.component.css'
})
export class PagesComponent implements OnInit {
  private fb = inject(FormBuilder);
  sidebarOpen = signal(true); loading = signal(true); saving = signal(false);
  pages = signal<Page[]>([]); editingPage = signal<Page | null>(null); showCreateModal = signal(false);
  pageForm = this.fb.group({ title: ['', Validators.required], slug: ['', Validators.required], content: ['', Validators.required], isPublished: [false] });
  ngOnInit() { this.loadPages(); }
  loadPages() { this.loading.set(true); setTimeout(() => { this.pages.set([{ id: '1', title: 'About Us', slug: 'about-us', content: 'About us content...', isPublished: true, updatedAt: '2024-03-10T10:00:00Z' }, { id: '2', title: 'Terms of Service', slug: 'terms', content: 'Terms content...', isPublished: true, updatedAt: '2024-03-05T10:00:00Z' }, { id: '3', title: 'Privacy Policy', slug: 'privacy', content: 'Privacy content...', isPublished: true, updatedAt: '2024-03-01T10:00:00Z' }, { id: '4', title: 'Contact Us', slug: 'contact', content: 'Contact content...', isPublished: true, updatedAt: '2024-02-28T10:00:00Z' }]); this.loading.set(false); }, 500); }
  editPage(page: Page) { this.editingPage.set(page); this.pageForm.patchValue(page); this.showCreateModal.set(true); }
  deletePage(page: Page) { if (confirm('Delete this page?')) { this.pages.update(pages => pages.filter(p => p.id !== page.id)); } }
  closeModal() { this.showCreateModal.set(false); this.editingPage.set(null); this.pageForm.reset(); }
  savePage() { if (this.pageForm.invalid) return; this.saving.set(true); setTimeout(() => { this.saving.set(false); this.closeModal(); this.loadPages(); }, 500); }
}
