import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { ContentService, ContentItemDto } from '@services/content.service';

@Component({
  selector: 'app-content',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent],
  templateUrl: './content.component.html',
  styleUrl: './content.component.css'
})
export class ContentComponent implements OnInit {
  private readonly contentService = inject(ContentService);
  
  content = signal<ContentItemDto[]>([]);
  featuredContent = signal<ContentItemDto | null>(null);
  loading = signal(true);
  activeCategory = signal('All');
  
  categories = ['All', 'News', 'Guides', 'Tips', 'Product Updates', 'Lifestyle'];
  
  filteredContent = () => {
    const cat = this.activeCategory();
    if (cat === 'All') return this.content().filter(c => c.id !== this.featuredContent()?.id);
    return this.content().filter(c => c.category === cat && c.id !== this.featuredContent()?.id);
  };
  
  ngOnInit() {
    this.loadContent();
  }
  
  loadContent() {
    this.loading.set(true);
    
    this.contentService.getPublishedContent().subscribe({
      next: (content) => {
        this.content.set(content);
        if (content.length > 0) this.featuredContent.set(content[0]);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        const mock: ContentItemDto[] = [
          { id: '1', title: '10 Tips for Smart Shopping', slug: 'smart-shopping-tips', category: 'Tips', excerpt: 'Learn how to save money and shop smarter with these proven strategies.', author: 'Sarah Johnson', status: 'Published', publishedAt: new Date(Date.now() - 86400000).toISOString(), readTime: 5, createdAt: new Date().toISOString() },
          { id: '2', title: 'Summer Collection 2024 is Here!', slug: 'summer-collection-2024', category: 'News', excerpt: 'Discover our exciting new summer collection featuring the latest trends.', author: 'Mike Chen', status: 'Published', publishedAt: new Date(Date.now() - 172800000).toISOString(), readTime: 3, createdAt: new Date().toISOString() },
          { id: '3', title: 'How to Choose the Perfect Gift', slug: 'perfect-gift-guide', category: 'Guides', excerpt: 'A comprehensive guide to finding the ideal gift for any occasion.', author: 'Emily Davis', status: 'Published', publishedAt: new Date(Date.now() - 259200000).toISOString(), readTime: 8, createdAt: new Date().toISOString() },
          { id: '4', title: 'New Features in Your Account Dashboard', slug: 'new-dashboard-features', category: 'Product Updates', excerpt: 'We\'ve added exciting new features to help you manage your orders better.', author: 'Tech Team', status: 'Published', publishedAt: new Date(Date.now() - 345600000).toISOString(), readTime: 4, createdAt: new Date().toISOString() },
          { id: '5', title: 'Sustainable Shopping: A Beginner\'s Guide', slug: 'sustainable-shopping', category: 'Lifestyle', excerpt: 'Make eco-friendly choices without compromising on style or quality.', author: 'Green Living', status: 'Published', publishedAt: new Date(Date.now() - 432000000).toISOString(), readTime: 6, createdAt: new Date().toISOString() },
          { id: '6', title: 'Maximizing Your Rewards Points', slug: 'maximize-rewards', category: 'Tips', excerpt: 'Get the most out of our loyalty program with these insider tips.', author: 'Sarah Johnson', status: 'Published', publishedAt: new Date(Date.now() - 518400000).toISOString(), readTime: 4, createdAt: new Date().toISOString() }
        ];
        this.content.set(mock);
        this.featuredContent.set(mock[0]);
      }
    });
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
