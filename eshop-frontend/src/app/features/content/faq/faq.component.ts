import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';

interface FAQItem {
  id: string;
  question: string;
  answer: string;
  category: string;
}

@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './faq.component.html',
  styleUrl: './faq.component.css'
})
export class FaqComponent implements OnInit {
  faqs = signal<FAQItem[]>([]);
  activeCategory = signal('All');
  expandedIds = signal<Set<string>>(new Set());
  searchQuery = '';
  
  categories = ['All', 'Orders', 'Shipping', 'Returns', 'Payments', 'Account'];
  
  ngOnInit() { this.loadFaqs(); }
  
  loadFaqs() {
    this.faqs.set([
      { id: '1', question: 'How do I track my order?', answer: 'You can track your order by logging into your account and visiting the "Orders" section. Click on the specific order to see real-time tracking information. You\'ll also receive email updates with tracking links.', category: 'Orders' },
      { id: '2', question: 'What payment methods do you accept?', answer: 'We accept all major credit cards (Visa, MasterCard, American Express), PayPal, Apple Pay, Google Pay, and bank transfers. All payments are processed securely through encrypted connections.', category: 'Payments' },
      { id: '3', question: 'How long does shipping take?', answer: 'Standard shipping typically takes 5-7 business days. Express shipping takes 2-3 business days. International shipping can take 10-14 business days depending on the destination country and customs processing.', category: 'Shipping' },
      { id: '4', question: 'What is your return policy?', answer: 'We offer a 30-day return policy for most items. Products must be unused and in their original packaging. Some items like personalized products or perishables cannot be returned. Refunds are processed within 5-7 business days after we receive the item.', category: 'Returns' },
      { id: '5', question: 'How do I reset my password?', answer: 'Click on "Login" and then "Forgot Password". Enter your email address and we\'ll send you a password reset link. The link expires after 24 hours for security reasons.', category: 'Account' },
      { id: '6', question: 'Can I change or cancel my order?', answer: 'You can modify or cancel your order within 1 hour of placing it. After that, the order enters processing and cannot be changed. Contact our support team immediately if you need assistance.', category: 'Orders' },
      { id: '7', question: 'Do you offer international shipping?', answer: 'Yes, we ship to over 100 countries worldwide. International shipping rates and delivery times vary by destination. Import duties and taxes may apply and are the responsibility of the recipient.', category: 'Shipping' },
      { id: '8', question: 'How do I use a promo code?', answer: 'Enter your promo code in the "Discount Code" field during checkout and click "Apply". The discount will be reflected in your order total. Note that only one promo code can be used per order.', category: 'Payments' },
      { id: '9', question: 'What if my item arrives damaged?', answer: 'Contact us within 48 hours of delivery with photos of the damage. We\'ll arrange a free replacement or full refund. Keep the original packaging as it may be needed for claims.', category: 'Returns' },
      { id: '10', question: 'How do I update my account information?', answer: 'Log into your account and go to "Profile Settings". From there you can update your name, email, phone number, and shipping addresses. Changes are saved immediately.', category: 'Account' }
    ]);
  }
  
  filteredFaqs() {
    return this.faqs().filter(faq => {
      const matchesCategory = this.activeCategory() === 'All' || faq.category === this.activeCategory();
      const matchesSearch = !this.searchQuery || faq.question.toLowerCase().includes(this.searchQuery.toLowerCase()) || faq.answer.toLowerCase().includes(this.searchQuery.toLowerCase());
      return matchesCategory && matchesSearch;
    });
  }
  
  toggleFaq(id: string) {
    this.expandedIds.update(ids => {
      const newIds = new Set(ids);
      if (newIds.has(id)) { newIds.delete(id); } else { newIds.add(id); }
      return newIds;
    });
  }
}
