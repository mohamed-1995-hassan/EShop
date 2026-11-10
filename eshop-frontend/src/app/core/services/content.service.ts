import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// Banner DTOs
export interface BannerDto {
  id: string;
  name: string;
  title: string;
  subtitle?: string;
  imageUrl: string;
  mobileImageUrl?: string;
  linkUrl?: string;
  position: BannerPosition;
  isPublished: boolean;
  startDate?: string;
  endDate?: string;
  clickCount: number;
  createdAt: string;
}

export type BannerPosition = 'Hero' | 'Sidebar' | 'Footer' | 'Category' | 'Popup';

export interface CreateBannerRequest {
  name: string;
  title: string;
  imageUrl: string;
  position: BannerPosition;
  linkUrl?: string;
}

export interface UpdateBannerRequest {
  title: string;
  subtitle?: string;
  imageUrl: string;
  mobileImageUrl?: string;
  linkUrl?: string;
}

export interface ScheduleBannerRequest {
  startDate?: string;
  endDate?: string;
}

// Landing Page DTOs
export interface LandingPageDto {
  id: string;
  title: string;
  slug: string;
  templateId?: string;
  blocks: ContentBlockDto[];
  isPublished: boolean;
  publishedAt?: string;
  createdAt: string;
}

export interface ContentBlockDto {
  id: string;
  type: BlockType;
  content: string;
  position: number;
}

export type BlockType = 'Text' | 'Image' | 'Video' | 'ProductGrid' | 'Banner' | 'HTML' | 'Testimonial';

export interface CreatePageRequest {
  title: string;
  slug: string;
  templateId?: string;
}

export interface AddBlockRequest {
  type: BlockType;
  content: string;
  position: number;
}

// FAQ DTOs
export interface FaqCategoryDto {
  id: string;
  name: string;
  description?: string;
  items: FaqItemDto[];
  sortOrder: number;
}

export interface FaqItemDto {
  id: string;
  question: string;
  answer: string;
  helpfulCount: number;
  notHelpfulCount: number;
  sortOrder: number;
}

export interface CreateFaqCategoryRequest {
  name: string;
  description?: string;
}

export interface AddFaqItemRequest {
  question: string;
  answer: string;
}

export interface MarkHelpfulRequest {
  helpful: boolean;
}

// Blog DTOs
export interface BlogPostDto {
  id: string;
  title: string;
  slug: string;
  excerpt?: string;
  content: string;
  featuredImageUrl?: string;
  authorId: string;
  authorName: string;
  tags: string[];
  isPublished: boolean;
  publishedAt?: string;
  viewCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateBlogPostRequest {
  title: string;
  content: string;
  authorId: string;
  authorName: string;
}

export interface UpdateBlogPostRequest {
  title: string;
  excerpt: string;
  content: string;
  featuredImageUrl?: string;
}

export interface SetTagsRequest {
  tags: string[];
}

@Injectable({ providedIn: 'root' })
export class ContentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/content`;

  // === BANNERS ===

  // GET /api/content/banners/{id}
  getBanner(id: string): Observable<BannerDto> {
    return this.http.get<BannerDto>(`${this.baseUrl}/banners/${id}`);
  }

  // GET /api/content/banners/active
  getActiveBanners(position: BannerPosition): Observable<BannerDto[]> {
    const params = new HttpParams().set('position', position);
    return this.http.get<BannerDto[]>(`${this.baseUrl}/banners/active`, { params });
  }

  // POST /api/content/banners
  createBanner(request: CreateBannerRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/banners`, request);
  }

  // PUT /api/content/banners/{id}
  updateBanner(id: string, request: UpdateBannerRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/banners/${id}`, request);
  }

  // PUT /api/content/banners/{id}/schedule
  scheduleBanner(id: string, request: ScheduleBannerRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/banners/${id}/schedule`, request);
  }

  // POST /api/content/banners/{id}/publish
  publishBanner(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/banners/${id}/publish`, {});
  }

  // POST /api/content/banners/{id}/unpublish
  unpublishBanner(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/banners/${id}/unpublish`, {});
  }

  // POST /api/content/banners/{id}/click
  recordBannerClick(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/banners/${id}/click`, {});
  }

  // === LANDING PAGES ===

  // GET /api/content/pages/{id}
  getPage(id: string): Observable<LandingPageDto> {
    return this.http.get<LandingPageDto>(`${this.baseUrl}/pages/${id}`);
  }

  // GET /api/content/pages/slug/{slug}
  getPageBySlug(slug: string): Observable<LandingPageDto> {
    return this.http.get<LandingPageDto>(`${this.baseUrl}/pages/slug/${slug}`);
  }

  // POST /api/content/pages
  createPage(request: CreatePageRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/pages`, request);
  }

  // POST /api/content/pages/{id}/blocks
  addBlock(pageId: string, request: AddBlockRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/pages/${pageId}/blocks`, request);
  }

  // POST /api/content/pages/{id}/publish
  publishPage(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/pages/${id}/publish`, {});
  }

  // === FAQ ===

  // GET /api/content/faq
  getFaqCategories(): Observable<FaqCategoryDto[]> {
    return this.http.get<FaqCategoryDto[]>(`${this.baseUrl}/faq`);
  }

  // POST /api/content/faq/categories
  createFaqCategory(request: CreateFaqCategoryRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/faq/categories`, request);
  }

  // POST /api/content/faq/categories/{categoryId}/items
  addFaqItem(categoryId: string, request: AddFaqItemRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/faq/categories/${categoryId}/items`, request);
  }

  // POST /api/content/faq/categories/{categoryId}/items/{itemId}/helpful
  markFaqHelpful(categoryId: string, itemId: string, request: MarkHelpfulRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/faq/categories/${categoryId}/items/${itemId}/helpful`, request);
  }

  // === BLOG ===

  // GET /api/content/blog
  getBlogPosts(skip = 0, take = 10): Observable<BlogPostDto[]> {
    const params = new HttpParams()
      .set('skip', skip.toString())
      .set('take', take.toString());
    return this.http.get<BlogPostDto[]>(`${this.baseUrl}/blog`, { params });
  }

  // GET /api/content/blog/slug/{slug}
  getBlogPostBySlug(slug: string): Observable<BlogPostDto> {
    return this.http.get<BlogPostDto>(`${this.baseUrl}/blog/slug/${slug}`);
  }

  // GET /api/content/blog/featured
  getFeaturedBlogPosts(count = 5): Observable<BlogPostDto[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<BlogPostDto[]>(`${this.baseUrl}/blog/featured`, { params });
  }

  // POST /api/content/blog
  createBlogPost(request: CreateBlogPostRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/blog`, request);
  }

  // PUT /api/content/blog/{id}
  updateBlogPost(id: string, request: UpdateBlogPostRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/blog/${id}`, request);
  }

  // POST /api/content/blog/{id}/publish
  publishBlogPost(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/blog/${id}/publish`, {});
  }

  // PUT /api/content/blog/{id}/tags
  setBlogTags(id: string, request: SetTagsRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/blog/${id}/tags`, request);
  }
}
