import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';

interface BlogPost {
  id: string;
  title: string;
  slug: string;
  excerpt: string;
  content: string;
  coverImage: string;
  author: { name: string; avatar?: string };
  category: string;
  tags: string[];
  publishedAt: string;
  readTime: number;
}

@Component({
  selector: 'app-blog',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent, LoadingComponent],
  templateUrl: './blog.component.html',
  styleUrl: './blog.component.css'
})
export class BlogComponent implements OnInit {
  loading = signal(true);
  posts = signal<BlogPost[]>([]);
  featuredPost = signal<BlogPost | null>(null);
  filterCategory = signal('');
  searchQuery = '';
  
  categories = ['Tutorials', 'Tips & Tricks', 'Product Reviews', 'Industry News'];
  
  ngOnInit() { this.loadPosts(); }
  
  loadPosts() {
    this.loading.set(true);
    setTimeout(() => {
      const posts: BlogPost[] = [
        { id: '1', title: 'The Ultimate Guide to Online Shopping in 2024', slug: 'ultimate-guide-online-shopping-2024', excerpt: 'Discover the best practices for secure and smart online shopping this year.', content: '', coverImage: 'https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?w=600', author: { name: 'Sarah Johnson' }, category: 'Tips & Tricks', tags: ['shopping', 'guide'], publishedAt: '2024-03-10T10:00:00Z', readTime: 8 },
        { id: '2', title: '10 Must-Have Tech Gadgets for Your Home Office', slug: '10-must-have-tech-gadgets-home-office', excerpt: 'Transform your workspace with these essential productivity boosters.', content: '', coverImage: 'https://images.unsplash.com/photo-1593642632559-0c6d3fc62b89?w=600', author: { name: 'Mike Chen' }, category: 'Product Reviews', tags: ['tech', 'home office'], publishedAt: '2024-03-08T10:00:00Z', readTime: 6 },
        { id: '3', title: 'How to Spot Fake Products Online', slug: 'how-to-spot-fake-products-online', excerpt: 'Learn the telltale signs of counterfeit items and protect yourself.', content: '', coverImage: 'https://images.unsplash.com/photo-1563013544-824ae1b704d3?w=600', author: { name: 'Emily Davis' }, category: 'Tips & Tricks', tags: ['safety', 'shopping'], publishedAt: '2024-03-05T10:00:00Z', readTime: 5 },
        { id: '4', title: 'E-commerce Trends to Watch in 2024', slug: 'ecommerce-trends-2024', excerpt: 'Stay ahead of the curve with these emerging industry trends.', content: '', coverImage: 'https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=600', author: { name: 'Alex Thompson' }, category: 'Industry News', tags: ['trends', 'ecommerce'], publishedAt: '2024-03-01T10:00:00Z', readTime: 7 },
        { id: '5', title: 'Setting Up Your First Online Store: A Beginner\'s Guide', slug: 'setting-up-first-online-store', excerpt: 'Everything you need to know to launch your e-commerce business.', content: '', coverImage: 'https://images.unsplash.com/photo-1556742111-a301076d9d18?w=600', author: { name: 'Sarah Johnson' }, category: 'Tutorials', tags: ['tutorial', 'ecommerce'], publishedAt: '2024-02-28T10:00:00Z', readTime: 12 }
      ];
      this.posts.set(posts);
      this.featuredPost.set(posts[0]);
      this.loading.set(false);
    }, 500);
  }
  
  filteredPosts() {
    return this.posts().filter(p => {
      if (this.featuredPost()?.id === p.id && !this.filterCategory() && !this.searchQuery) return false;
      const matchesCategory = !this.filterCategory() || p.category === this.filterCategory();
      const matchesSearch = !this.searchQuery || p.title.toLowerCase().includes(this.searchQuery.toLowerCase());
      return matchesCategory && matchesSearch;
    });
  }
}
