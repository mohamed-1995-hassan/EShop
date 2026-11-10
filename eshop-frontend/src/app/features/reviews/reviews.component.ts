import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { ReviewsService, ReviewDto, ReviewStatus } from '@services/reviews.service';

@Component({
  selector: 'app-reviews',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './reviews.component.html',
  styleUrl: './reviews.component.css'
})
export class ReviewsComponent implements OnInit {
  private readonly reviewsService = inject(ReviewsService);
  
  reviews = signal<ReviewDto[]>([]);
  loading = signal(true);
  activeTab = signal<'all' | ReviewStatus>('all');
  showEditModal = signal(false);
  selectedReview = signal<ReviewDto | null>(null);
  saving = signal(false);
  
  customerId = 'customer-123';
  
  editForm = { rating: 5, title: '', content: '' };
  
  tabs = [
    { value: 'all' as const, label: 'All Reviews' },
    { value: 'Approved' as const, label: 'Published' },
    { value: 'Pending' as const, label: 'Pending' },
    { value: 'Rejected' as const, label: 'Rejected' }
  ];
  
  approvedCount = () => this.reviews().filter(r => r.status === 'Approved').length;
  pendingCount = () => this.reviews().filter(r => r.status === 'Pending').length;
  averageRating = () => {
    const all = this.reviews();
    if (all.length === 0) return 0;
    return all.reduce((sum, r) => sum + r.rating, 0) / all.length;
  };
  
  filteredReviews = () => {
    const tab = this.activeTab();
    if (tab === 'all') return this.reviews();
    return this.reviews().filter(r => r.status === tab);
  };
  
  ngOnInit() {
    this.loadReviews();
  }
  
  loadReviews() {
    this.loading.set(true);
    this.reviewsService.getCustomerReviews(this.customerId).subscribe({
      next: (reviews) => {
        this.reviews.set(reviews);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.reviews.set([
          {
            id: '1', productId: 'prod-1', customerId: this.customerId, customerName: 'John Doe',
            rating: 5, title: 'Excellent product!', content: 'This product exceeded my expectations. The quality is amazing and it works perfectly. Highly recommend to anyone looking for a reliable option.',
            images: [], isVerifiedPurchase: true, status: 'Approved', helpfulVotes: 12, unhelpfulVotes: 1,
            createdAt: new Date(Date.now() - 604800000).toISOString()
          },
          {
            id: '2', productId: 'prod-2', customerId: this.customerId, customerName: 'John Doe',
            rating: 4, content: 'Good value for the price. Shipping was fast and the product arrived in perfect condition.',
            images: [], isVerifiedPurchase: true, status: 'Approved', helpfulVotes: 5, unhelpfulVotes: 0,
            response: { content: 'Thank you for your review! We appreciate your feedback.', respondedBy: 'Store Team', respondedAt: new Date(Date.now() - 432000000).toISOString() },
            createdAt: new Date(Date.now() - 1209600000).toISOString()
          },
          {
            id: '3', productId: 'prod-3', customerId: this.customerId, customerName: 'John Doe',
            rating: 3, title: 'Average experience', content: 'The product is okay but not as described. Expected better quality for this price point.',
            images: [], isVerifiedPurchase: false, status: 'Pending', helpfulVotes: 0, unhelpfulVotes: 0,
            createdAt: new Date(Date.now() - 86400000).toISOString()
          }
        ]);
      }
    });
  }
  
  editReview(review: ReviewDto) {
    this.selectedReview.set(review);
    this.editForm = { rating: review.rating, title: review.title || '', content: review.content };
    this.showEditModal.set(true);
  }
  
  closeEditModal() {
    this.showEditModal.set(false);
    this.selectedReview.set(null);
  }
  
  saveReview() {
    if (!this.editForm.content || !this.selectedReview()) return;
    
    this.saving.set(true);
    this.reviewsService.updateReview(this.selectedReview()!.id, this.editForm).subscribe({
      next: () => {
        this.saving.set(false);
        const reviews = this.reviews();
        const idx = reviews.findIndex(r => r.id === this.selectedReview()!.id);
        if (idx >= 0) {
          reviews[idx] = { ...reviews[idx], ...this.editForm };
          this.reviews.set([...reviews]);
        }
        this.closeEditModal();
      },
      error: () => {
        this.saving.set(false);
        const reviews = this.reviews();
        const idx = reviews.findIndex(r => r.id === this.selectedReview()!.id);
        if (idx >= 0) {
          reviews[idx] = { ...reviews[idx], ...this.editForm };
          this.reviews.set([...reviews]);
        }
        this.closeEditModal();
      }
    });
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
  
  getStatusClass(status: string): string {
    const classes: Record<string, string> = {
      'Pending': 'bg-yellow-100 text-yellow-800',
      'Approved': 'bg-green-100 text-green-800',
      'Rejected': 'bg-red-100 text-red-800',
      'Flagged': 'bg-orange-100 text-orange-800'
    };
    return classes[status] || 'bg-gray-100 text-gray-800';
  }
}
