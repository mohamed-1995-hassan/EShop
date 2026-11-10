import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SidebarComponent } from '@layout/sidebar/sidebar.component';
import { NavComponent } from '@layout/nav/nav.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';

interface BlogPost { id: string; title: string; slug: string; excerpt: string; coverImage: string; category: string; status: string; views: number; publishedAt?: string; updatedAt: string; }

@Component({
  selector: 'app-blog-management',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, SidebarComponent, NavComponent, LoadingComponent],
  templateUrl: './blog-management.component.html',
  styleUrl: './blog-management.component.css'
})
export class BlogManagementComponent implements OnInit {
  private fb = inject(FormBuilder);
  sidebarOpen = signal(true); loading = signal(true); saving = signal(false);
  posts = signal<BlogPost[]>([]); editingPost = signal<BlogPost | null>(null); showCreateModal = signal(false);
  postForm = this.fb.group({ title: ['', Validators.required], slug: ['', Validators.required], coverImage: [''], category: ['Tutorials'], status: ['Draft'], excerpt: [''], content: ['', Validators.required] });
  ngOnInit() { this.loadPosts(); }
  loadPosts() { this.loading.set(true); setTimeout(() => { this.posts.set([{ id: '1', title: 'Ultimate Guide to Online Shopping', slug: 'ultimate-guide', excerpt: 'Everything you need to know...', coverImage: 'https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?w=300', category: 'Tips & Tricks', status: 'Published', views: 1234, publishedAt: '2024-03-10T10:00:00Z', updatedAt: '2024-03-10T10:00:00Z' }, { id: '2', title: 'Top 10 Tech Gadgets', slug: 'top-10-tech-gadgets', excerpt: 'Must-have gadgets...', coverImage: 'https://images.unsplash.com/photo-1593642632559-0c6d3fc62b89?w=300', category: 'Product Reviews', status: 'Published', views: 856, publishedAt: '2024-03-08T10:00:00Z', updatedAt: '2024-03-08T10:00:00Z' }, { id: '3', title: 'E-commerce Trends 2024', slug: 'ecommerce-trends-2024', excerpt: 'Upcoming trends...', coverImage: '', category: 'Industry News', status: 'Draft', views: 0, updatedAt: '2024-03-12T10:00:00Z' }]); this.loading.set(false); }, 500); }
  getPublishedCount() { return this.posts().filter(p => p.status === 'Published').length; }
  getDraftCount() { return this.posts().filter(p => p.status === 'Draft').length; }
  getTotalViews() { return this.posts().reduce((sum, p) => sum + p.views, 0); }
  editPost(post: BlogPost) { this.editingPost.set(post); this.postForm.patchValue(post as any); this.showCreateModal.set(true); }
  deletePost(post: BlogPost) { if (confirm('Delete this post?')) { this.posts.update(posts => posts.filter(p => p.id !== post.id)); } }
  closeModal() { this.showCreateModal.set(false); this.editingPost.set(null); this.postForm.reset({ category: 'Tutorials', status: 'Draft' }); }
  savePost() { if (this.postForm.invalid) return; this.saving.set(true); setTimeout(() => { this.saving.set(false); this.closeModal(); this.loadPosts(); }, 500); }
}
