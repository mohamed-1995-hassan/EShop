import { Routes } from '@angular/router';
import { authGuard } from '@core/guards/auth.guard';
import { adminGuard } from '@core/guards/admin.guard';

export const routes: Routes = [
  // Public routes
  {
    path: '',
    loadComponent: () => import('@features/dashboard/dashboard.component').then(m => m.DashboardComponent),
    title: 'EShop - Home'
  },
  
  // Auth routes
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () => import('@features/auth/login/login.component').then(m => m.LoginComponent),
        title: 'Login - EShop'
      },
      {
        path: 'register',
        loadComponent: () => import('@features/auth/register/register.component').then(m => m.RegisterComponent),
        title: 'Register - EShop'
      },
      {
        path: 'forgot-password',
        loadComponent: () => import('@features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
        title: 'Forgot Password - EShop'
      }
    ]
  },
  
  // Product routes
  {
    path: 'products',
    children: [
      {
        path: '',
        loadComponent: () => import('@features/products/product-list/product-list.component').then(m => m.ProductListComponent),
        title: 'Products - EShop'
      },
      {
        path: 'search',
        loadComponent: () => import('@features/products/product-search/product-search.component').then(m => m.ProductSearchComponent),
        title: 'Search Products - EShop'
      },
      {
        path: ':id',
        loadComponent: () => import('@features/products/product-detail/product-detail.component').then(m => m.ProductDetailComponent),
        title: 'Product Details - EShop'
      }
    ]
  },
  
  // Categories
  {
    path: 'categories',
    loadComponent: () => import('@features/categories/categories.component').then(m => m.CategoriesComponent),
    title: 'Categories - EShop'
  },
  
  // Cart
  {
    path: 'cart',
    loadComponent: () => import('@features/cart/cart.component').then(m => m.CartComponent),
    title: 'Shopping Cart - EShop'
  },
  
  // Checkout (requires auth)
  {
    path: 'checkout',
    loadComponent: () => import('@features/orders/checkout/checkout.component').then(m => m.CheckoutComponent),
    canActivate: [authGuard],
    title: 'Checkout - EShop'
  },
  
  // User account routes (requires auth)
  {
    path: 'account',
    canActivate: [authGuard],
    children: [
      {
        path: 'profile',
        loadComponent: () => import('@features/customers/profile/profile.component').then(m => m.ProfileComponent),
        title: 'My Profile - EShop'
      },
      {
        path: 'addresses',
        loadComponent: () => import('@features/customers/addresses/addresses.component').then(m => m.AddressesComponent),
        title: 'My Addresses - EShop'
      },
      {
        path: 'orders',
        loadComponent: () => import('@features/orders/order-list/order-list.component').then(m => m.OrderListComponent),
        title: 'My Orders - EShop'
      },
      {
        path: 'orders/:id',
        loadComponent: () => import('@features/orders/order-detail/order-detail.component').then(m => m.OrderDetailComponent),
        title: 'Order Details - EShop'
      },
      {
        path: 'wishlist',
        loadComponent: () => import('@features/wishlists/wishlists.component').then(m => m.WishlistsComponent),
        title: 'My Wishlist - EShop'
      },
      {
        path: 'reviews',
        loadComponent: () => import('@features/reviews/reviews.component').then(m => m.ReviewsComponent),
        title: 'My Reviews - EShop'
      },
      {
        path: 'subscriptions',
        loadComponent: () => import('@features/subscriptions/subscriptions.component').then(m => m.SubscriptionsComponent),
        title: 'My Subscriptions - EShop'
      },
      {
        path: 'loyalty',
        loadComponent: () => import('@features/loyalty/loyalty.component').then(m => m.LoyaltyComponent),
        title: 'Loyalty Program - EShop'
      },
      {
        path: 'gift-cards',
        loadComponent: () => import('@features/gift-cards/gift-cards.component').then(m => m.GiftCardsComponent),
        title: 'Gift Cards - EShop'
      },
      {
        path: 'returns',
        loadComponent: () => import('@features/returns/returns.component').then(m => m.ReturnsComponent),
        title: 'Returns - EShop'
      },
      {
        path: 'support',
        loadComponent: () => import('@features/support/support.component').then(m => m.SupportComponent),
        title: 'Support - EShop'
      }
    ]
  },
  
  // Auctions
  {
    path: 'auctions',
    loadComponent: () => import('@features/auctions/auctions.component').then(m => m.AuctionsComponent),
    title: 'Auctions - EShop'
  },
  
  // Bundles
  {
    path: 'bundles',
    loadComponent: () => import('@features/bundles/bundles.component').then(m => m.BundlesComponent),
    title: 'Product Bundles - EShop'
  },
  
  // Digital Products
  {
    path: 'digital-products',
    loadComponent: () => import('@features/digital-products/digital-products.component').then(m => m.DigitalProductsComponent),
    title: 'Digital Products - EShop'
  },
  
  // Content pages
  {
    path: 'faq',
    loadComponent: () => import('@features/content/faq/faq.component').then(m => m.FaqComponent),
    title: 'FAQ - EShop'
  },
  {
    path: 'blog',
    loadComponent: () => import('@features/content/blog/blog.component').then(m => m.BlogComponent),
    title: 'Blog - EShop'
  },
  
  // Vendor routes
  {
    path: 'vendor',
    canActivate: [authGuard],
    loadChildren: () => import('@features/vendors/vendor.routes').then(m => m.VENDOR_ROUTES)
  },
  
  // Affiliate routes
  {
    path: 'affiliate',
    canActivate: [authGuard],
    loadComponent: () => import('@features/affiliates/affiliates.component').then(m => m.AffiliatesComponent),
    title: 'Affiliate Program - EShop'
  },
  
  // Admin routes
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('@features/admin/dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent),
        title: 'Admin Dashboard - EShop'
      },
      {
        path: 'customers',
        loadComponent: () => import('@features/admin/customers/admin-customers.component').then(m => m.AdminCustomersComponent),
        title: 'Manage Customers - EShop'
      },
      {
        path: 'orders',
        loadComponent: () => import('@features/admin/orders/admin-orders.component').then(m => m.AdminOrdersComponent),
        title: 'Manage Orders - EShop'
      },
      {
        path: 'products',
        loadComponent: () => import('@features/admin/products/admin-products.component').then(m => m.AdminProductsComponent),
        title: 'Manage Products - EShop'
      },
      {
        path: 'users',
        loadComponent: () => import('@features/admin/users/admin-users.component').then(m => m.AdminUsersComponent),
        title: 'Manage Users - EShop'
      },
      {
        path: 'settings',
        loadComponent: () => import('@features/admin/settings/admin-settings.component').then(m => m.AdminSettingsComponent),
        title: 'Settings - EShop'
      },
      {
        path: 'analytics',
        loadComponent: () => import('@features/analytics/analytics.component').then(m => m.AnalyticsComponent),
        title: 'Analytics - EShop'
      },
      {
        path: 'coupons',
        loadComponent: () => import('@features/coupons/coupons.component').then(m => m.CouponsComponent),
        title: 'Manage Coupons - EShop'
      },
      {
        path: 'promotions',
        loadComponent: () => import('@features/promotions/promotions.component').then(m => m.PromotionsComponent),
        title: 'Manage Promotions - EShop'
      },
      {
        path: 'content',
        loadChildren: () => import('@features/content/content.routes').then(m => m.CONTENT_ROUTES)
      },
      {
        path: 'suppliers',
        loadComponent: () => import('@features/suppliers/suppliers.component').then(m => m.SuppliersComponent),
        title: 'Manage Suppliers - EShop'
      },
      {
        path: 'tax',
        loadComponent: () => import('@features/tax/tax.component').then(m => m.TaxComponent),
        title: 'Tax Configuration - EShop'
      },
      {
        path: 'currency',
        loadComponent: () => import('@features/currency/currency.component').then(m => m.CurrencyComponent),
        title: 'Currency Configuration - EShop'
      },
      {
        path: 'shipments',
        loadComponent: () => import('@features/shipments/shipments.component').then(m => m.ShipmentsComponent),
        title: 'Manage Shipments - EShop'
      }
    ]
  },
  
  // 404 Not Found
  {
    path: '**',
    loadComponent: () => import('@shared/components/not-found/not-found.component').then(m => m.NotFoundComponent),
    title: 'Page Not Found - EShop'
  }
];
