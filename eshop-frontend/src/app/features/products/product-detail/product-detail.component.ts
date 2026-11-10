import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { ProductsService, ProductDetailsDto } from '@core/services/products.service';
import { ReviewsService, ReviewDto, ProductReviewsDto } from '@core/services/reviews.service';
import { CartService } from '@core/services/cart.service';
import { Product, Review } from '@shared/models';
import { catchError, of, forkJoin } from 'rxjs';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent, LoadingComponent],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css'
})
export class ProductDetailComponent implements OnInit {
  private productsService = inject(ProductsService);
  private reviewsService = inject(ReviewsService);
  private cartService = inject(CartService);
  private route = inject(ActivatedRoute);
  
  product = signal<Product | null>(null);
  reviews = signal<Review[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  
  selectedImage = signal<string | null>(null);
  selectedVariant = signal<any>(null);
  quantity = signal(1);
  isInWishlist = signal(false);
  
  maxQuantity = computed(() => {
    const variant = this.selectedVariant();
    if (variant) return variant.quantity;
    return this.product()?.quantity ?? 0;
  });
  
  ngOnInit() {
    const productId = this.route.snapshot.paramMap.get('id');
    if (productId) {
      this.loadProduct(productId);
    }
  }
  
  loadProduct(id: string) {
    this.loading.set(true);
    this.error.set(null);
    
    forkJoin({
      product: this.productsService.getProduct(id).pipe(
        catchError(err => {
          console.error('Error loading product:', err);
          return of(null);
        })
      ),
      reviews: this.reviewsService.getProductReviews(id, 0, 10, 'newest').pipe(
        catchError(err => {
          console.error('Error loading reviews:', err);
          return of(null);
        })
      )
    }).subscribe({
      next: ({ product, reviews }) => {
        if (product) {
          this.product.set(this.mapProductFromDto(product));
          this.selectedImage.set(product.images?.[0] || null);
        }
        
        if (reviews) {
          this.reviews.set(reviews.reviews.map(r => this.mapReviewFromDto(r)));
        }
        
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load product');
        this.loading.set(false);
      }
    });
  }
  
  private mapProductFromDto(dto: ProductDetailsDto): Product {
    return {
      id: dto.id,
      name: dto.name,
      slug: dto.sku.toLowerCase().replace(/\s+/g, '-'),
      description: dto.description,
      sku: dto.sku,
      price: dto.price,
      quantity: dto.stock,
      categoryId: dto.categories?.[0] || '',
      images: dto.images?.map((url, idx) => ({
        id: `${dto.id}-${idx}`,
        url,
        sortOrder: idx,
        isMain: idx === 0
      })) || [],
      mainImage: dto.images?.[0],
      isActive: dto.isActive,
      isFeatured: false,
      isDigital: false,
      createdAt: dto.createdAt,
      updatedAt: dto.updatedAt
    };
  }
  
  private mapReviewFromDto(dto: ReviewDto): Review {
    return {
      id: dto.id,
      productId: dto.productId,
      customerId: dto.customerId,
      customerName: dto.customerName,
      rating: dto.rating,
      title: dto.title,
      content: dto.content,
      images: dto.images,
      isVerifiedPurchase: dto.isVerifiedPurchase,
      isApproved: dto.status === 'Approved',
      helpfulCount: dto.helpfulVotes,
      createdAt: dto.createdAt
    };
  }
  
  selectImage(url: string) {
    this.selectedImage.set(url);
  }
  
  selectVariant(variant: any) {
    this.selectedVariant.set(variant);
    if (variant.image) {
      this.selectedImage.set(variant.image);
    }
  }
  
  setQuantity(value: number) {
    if (value >= 1 && value <= this.maxQuantity()) {
      this.quantity.set(value);
    }
  }
  
  incrementQuantity() {
    if (this.quantity() < this.maxQuantity()) {
      this.quantity.update(q => q + 1);
    }
  }
  
  decrementQuantity() {
    if (this.quantity() > 1) {
      this.quantity.update(q => q - 1);
    }
  }
  
  addToCart() {
    const product = this.product();
    if (product) {
      this.cartService.quickAdd({
        id: product.id,
        name: product.name,
        price: this.selectedVariant()?.price || product.price,
        image: product.mainImage,
        sku: this.selectedVariant()?.sku || product.sku
      }, this.quantity());
    }
  }
  
  addToWishlist() {
    this.isInWishlist.update(v => !v);
  }
}
