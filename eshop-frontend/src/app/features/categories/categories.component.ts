import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { CategoriesService, CategorySummaryDto } from '@core/services/categories.service';
import { Category } from '@shared/models';
import { catchError, of, forkJoin } from 'rxjs';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.css'
})
export class CategoriesComponent implements OnInit {
  private categoriesService = inject(CategoriesService);
  
  categories = signal<Category[]>([]);
  featuredCategories = signal<Category[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  
  ngOnInit() {
    this.loadCategories();
  }
  
  private loadCategories() {
    this.loading.set(true);
    
    forkJoin({
      root: this.categoriesService.getRootCategories().pipe(
        catchError(err => {
          console.error('Error loading root categories:', err);
          return of([]);
        })
      ),
      featured: this.categoriesService.getFeaturedCategories().pipe(
        catchError(err => {
          console.error('Error loading featured categories:', err);
          return of([]);
        })
      )
    }).subscribe({
      next: ({ root, featured }) => {
        this.categories.set(root.map(cat => this.mapCategoryFromDto(cat)));
        this.featuredCategories.set(featured.map(cat => this.mapCategoryFromDto(cat)));
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load categories');
        this.loading.set(false);
      }
    });
  }
  
  private mapCategoryFromDto(dto: CategorySummaryDto): Category {
    return {
      id: dto.id,
      name: dto.name,
      slug: dto.slug,
      image: dto.imageUrl,
      productCount: dto.productCount,
      sortOrder: 0,
      isActive: dto.isActive
    };
  }
}
