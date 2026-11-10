import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';

interface FAQ { id: string; question: string; answer: string; category: string; sortOrder: number; isPublished: boolean; }

@Component({
  selector: 'app-faq-management',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './faq-management.component.html',
  styleUrl: './faq-management.component.css'
})
export class FaqManagementComponent implements OnInit {
  private fb = inject(FormBuilder);
  sidebarOpen = signal(true); loading = signal(true); saving = signal(false);
  faqs = signal<FAQ[]>([]); editingFaq = signal<FAQ | null>(null); showCreateModal = signal(false); filterCategory = signal('All');
  categories = ['All', 'Orders', 'Shipping', 'Returns', 'Payments', 'Account'];
  faqForm = this.fb.group({ question: ['', Validators.required], answer: ['', Validators.required], category: ['Orders'], sortOrder: [0], isPublished: [true] });
  ngOnInit() { this.loadFaqs(); }
  loadFaqs() { this.loading.set(true); setTimeout(() => { this.faqs.set([{ id: '1', question: 'How do I track my order?', answer: 'You can track your order by logging into your account...', category: 'Orders', sortOrder: 1, isPublished: true }, { id: '2', question: 'What payment methods do you accept?', answer: 'We accept all major credit cards...', category: 'Payments', sortOrder: 1, isPublished: true }, { id: '3', question: 'How long does shipping take?', answer: 'Standard shipping takes 5-7 business days...', category: 'Shipping', sortOrder: 1, isPublished: true }, { id: '4', question: 'What is your return policy?', answer: 'We offer a 30-day return policy...', category: 'Returns', sortOrder: 1, isPublished: true }]); this.loading.set(false); }, 500); }
  filteredFaqs() { return this.faqs().filter(f => this.filterCategory() === 'All' || f.category === this.filterCategory()); }
  editFaq(faq: FAQ) { this.editingFaq.set(faq); this.faqForm.patchValue(faq); this.showCreateModal.set(true); }
  togglePublish(faq: FAQ) { this.faqs.update(faqs => faqs.map(f => f.id === faq.id ? { ...f, isPublished: !f.isPublished } : f)); }
  deleteFaq(faq: FAQ) { if (confirm('Delete this FAQ?')) { this.faqs.update(faqs => faqs.filter(f => f.id !== faq.id)); } }
  closeModal() { this.showCreateModal.set(false); this.editingFaq.set(null); this.faqForm.reset({ category: 'Orders', sortOrder: 0, isPublished: true }); }
  saveFaq() { if (this.faqForm.invalid) return; this.saving.set(true); setTimeout(() => { this.saving.set(false); this.closeModal(); this.loadFaqs(); }, 500); }
}
