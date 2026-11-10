// Product Models
export interface Product {
  id: string;
  name: string;
  slug: string;
  description: string;
  shortDescription?: string;
  sku: string;
  price: number;
  compareAtPrice?: number;
  costPrice?: number;
  quantity: number;
  lowStockThreshold?: number;
  categoryId: string;
  categoryName?: string;
  brandId?: string;
  brandName?: string;
  vendorId?: string;
  vendorName?: string;
  images: ProductImage[];
  mainImage?: string;
  tags?: string[];
  attributes?: ProductAttribute[];
  variants?: ProductVariant[];
  isActive: boolean;
  isFeatured: boolean;
  isDigital: boolean;
  weight?: number;
  dimensions?: ProductDimensions;
  seoTitle?: string;
  seoDescription?: string;
  averageRating?: number;
  reviewCount?: number;
  createdAt: string;
  updatedAt: string;
}

export interface ProductImage {
  id: string;
  url: string;
  altText?: string;
  sortOrder: number;
  isMain: boolean;
}

export interface ProductAttribute {
  name: string;
  value: string;
}

export interface ProductVariant {
  id: string;
  sku: string;
  name: string;
  price: number;
  compareAtPrice?: number;
  quantity: number;
  attributes: ProductAttribute[];
  image?: string;
}

export interface ProductDimensions {
  length: number;
  width: number;
  height: number;
  unit: 'cm' | 'in';
}

// Category Models
export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  parentId?: string;
  parentName?: string;
  image?: string;
  icon?: string;
  sortOrder: number;
  isActive: boolean;
  productCount?: number;
  children?: Category[];
  breadcrumb?: Category[];
}

// Brand Models
export interface Brand {
  id: string;
  name: string;
  slug: string;
  description?: string;
  logo?: string;
  website?: string;
  isActive: boolean;
}

// Cart Models
export interface Cart {
  id: string;
  customerId?: string;
  items: CartItem[];
  subtotal: number;
  discount: number;
  tax: number;
  shipping: number;
  total: number;
  couponCode?: string;
  couponDiscount?: number;
  itemCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CartItem {
  id: string;
  productId: string;
  productName: string;
  productImage?: string;
  variantId?: string;
  variantName?: string;
  sku: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  attributes?: ProductAttribute[];
}

// Order Models
export interface Order {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName?: string;
  customerEmail?: string;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  shippingStatus: ShippingStatus;
  items: OrderItem[];
  billingAddress: Address;
  shippingAddress: Address;
  subtotal: number;
  discount: number;
  tax: number;
  shipping: number;
  total: number;
  couponCode?: string;
  paymentMethod?: string;
  shippingMethod?: string;
  notes?: string;
  trackingNumber?: string;
  estimatedDelivery?: string;
  createdAt: string;
  updatedAt: string;
  completedAt?: string;
}

export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  productImage?: string;
  variantId?: string;
  variantName?: string;
  sku: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  attributes?: ProductAttribute[];
}

export type OrderStatus = 'Pending' | 'Confirmed' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled' | 'Refunded';
export type PaymentStatus = 'Pending' | 'Authorized' | 'Paid' | 'PartiallyRefunded' | 'Refunded' | 'Failed';
export type ShippingStatus = 'Pending' | 'Processing' | 'Shipped' | 'InTransit' | 'OutForDelivery' | 'Delivered' | 'Failed';

// Customer Models
export interface Customer {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone?: string;
  avatar?: string;
  dateOfBirth?: string;
  gender?: 'Male' | 'Female' | 'Other';
  addresses?: Address[];
  defaultBillingAddressId?: string;
  defaultShippingAddressId?: string;
  loyaltyPoints?: number;
  loyaltyTier?: string;
  totalOrders?: number;
  totalSpent?: number;
  isActive: boolean;
  isVerified: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export interface Address {
  id: string;
  type: 'Billing' | 'Shipping' | 'Both';
  firstName: string;
  lastName: string;
  company?: string;
  address1: string;
  address2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  countryCode: string;
  phone?: string;
  isDefault: boolean;
}

// Review Models
export interface Review {
  id: string;
  productId: string;
  productName?: string;
  customerId: string;
  customerName?: string;
  rating: number;
  title?: string;
  content: string;
  pros?: string[];
  cons?: string[];
  images?: string[];
  isVerifiedPurchase: boolean;
  isApproved: boolean;
  helpfulCount: number;
  createdAt: string;
  updatedAt?: string;
  reply?: ReviewReply;
}

export interface ReviewReply {
  content: string;
  repliedBy: string;
  repliedAt: string;
}

// Wishlist Models
export interface Wishlist {
  id: string;
  name: string;
  customerId: string;
  items: WishlistItem[];
  isPublic: boolean;
  shareUrl?: string;
  createdAt: string;
}

export interface WishlistItem {
  id: string;
  productId: string;
  productName: string;
  productImage?: string;
  productPrice: number;
  productAvailability: boolean;
  addedAt: string;
}

// Coupon Models
export interface Coupon {
  id: string;
  code: string;
  name: string;
  description?: string;
  type: 'Percentage' | 'FixedAmount' | 'FreeShipping' | 'BuyXGetY';
  value: number;
  minPurchase?: number;
  maxDiscount?: number;
  usageLimit?: number;
  usageCount: number;
  perCustomerLimit?: number;
  validFrom: string;
  validUntil: string;
  isActive: boolean;
  applicableProducts?: string[];
  applicableCategories?: string[];
  excludedProducts?: string[];
  excludedCategories?: string[];
}

// Gift Card Models
export interface GiftCard {
  id: string;
  code: string;
  initialBalance: number;
  currentBalance: number;
  currency: string;
  recipientEmail?: string;
  recipientName?: string;
  senderName?: string;
  message?: string;
  isActive: boolean;
  expiresAt?: string;
  createdAt: string;
  usedAt?: string;
}

// Loyalty Models
export interface LoyaltyAccount {
  id: string;
  customerId: string;
  points: number;
  tier: LoyaltyTier;
  lifetimePoints: number;
  pointsToNextTier?: number;
  transactions: LoyaltyTransaction[];
}

export interface LoyaltyTier {
  id: string;
  name: string;
  minPoints: number;
  multiplier: number;
  benefits: string[];
}

export interface LoyaltyTransaction {
  id: string;
  type: 'Earned' | 'Redeemed' | 'Expired' | 'Adjusted';
  points: number;
  description: string;
  orderId?: string;
  createdAt: string;
}

// Subscription Models
export interface Subscription {
  id: string;
  customerId: string;
  productId: string;
  productName: string;
  variantId?: string;
  quantity: number;
  frequency: SubscriptionFrequency;
  status: SubscriptionStatus;
  price: number;
  discount?: number;
  nextBillingDate: string;
  nextShipDate: string;
  billingAddress: Address;
  shippingAddress: Address;
  paymentMethodId: string;
  createdAt: string;
  pausedAt?: string;
  cancelledAt?: string;
}

export type SubscriptionFrequency = 'Weekly' | 'BiWeekly' | 'Monthly' | 'BiMonthly' | 'Quarterly' | 'Yearly';
export type SubscriptionStatus = 'Active' | 'Paused' | 'Cancelled' | 'Expired';

// Shipment Models
export interface Shipment {
  id: string;
  orderId: string;
  orderNumber?: string;
  carrier: string;
  trackingNumber: string;
  trackingUrl?: string;
  status: ShippingStatus;
  estimatedDelivery?: string;
  actualDelivery?: string;
  weight?: number;
  dimensions?: ProductDimensions;
  shippingCost: number;
  labelUrl?: string;
  events: ShipmentEvent[];
  createdAt: string;
}

export interface ShipmentEvent {
  status: string;
  description: string;
  location?: string;
  timestamp: string;
}

// Return Models
export interface Return {
  id: string;
  orderId: string;
  orderNumber?: string;
  customerId: string;
  status: ReturnStatus;
  reason: string;
  reasonDetails?: string;
  items: ReturnItem[];
  refundAmount: number;
  refundMethod: 'Original' | 'StoreCredit' | 'Exchange';
  shippingLabelUrl?: string;
  trackingNumber?: string;
  createdAt: string;
  processedAt?: string;
  completedAt?: string;
}

export interface ReturnItem {
  id: string;
  orderItemId: string;
  productId: string;
  productName: string;
  quantity: number;
  reason: string;
  condition: 'New' | 'Opened' | 'Damaged';
}

export type ReturnStatus = 'Requested' | 'Approved' | 'Rejected' | 'Shipped' | 'Received' | 'Inspected' | 'Refunded' | 'Exchanged';

// Vendor Models
export interface Vendor {
  id: string;
  name: string;
  slug: string;
  email: string;
  phone?: string;
  description?: string;
  logo?: string;
  banner?: string;
  website?: string;
  address?: Address;
  rating?: number;
  reviewCount?: number;
  productCount?: number;
  orderCount?: number;
  totalSales?: number;
  commissionRate: number;
  isActive: boolean;
  isVerified: boolean;
  createdAt: string;
}

// Supplier Models
export interface Supplier {
  id: string;
  name: string;
  code: string;
  email: string;
  phone?: string;
  contactPerson?: string;
  address?: Address;
  website?: string;
  paymentTerms?: string;
  leadTime?: number;
  minOrderValue?: number;
  isActive: boolean;
  rating?: number;
  createdAt: string;
}

// Auction Models
export interface Auction {
  id: string;
  productId: string;
  productName: string;
  productImage?: string;
  startPrice: number;
  currentPrice: number;
  reservePrice?: number;
  buyNowPrice?: number;
  minBidIncrement: number;
  startTime: string;
  endTime: string;
  status: AuctionStatus;
  winnerId?: string;
  winnerName?: string;
  bidCount: number;
  watcherCount: number;
  bids?: AuctionBid[];
}

export interface AuctionBid {
  id: string;
  auctionId: string;
  customerId: string;
  customerName?: string;
  amount: number;
  maxBid?: number;
  isAutoBid: boolean;
  isWinning: boolean;
  createdAt: string;
}

export type AuctionStatus = 'Scheduled' | 'Active' | 'Ended' | 'Sold' | 'Cancelled' | 'NoSale';

// Bundle Models
export interface Bundle {
  id: string;
  name: string;
  slug: string;
  description?: string;
  image?: string;
  products: BundleProduct[];
  originalPrice: number;
  bundlePrice: number;
  savings: number;
  savingsPercentage: number;
  isActive: boolean;
  validFrom?: string;
  validUntil?: string;
  quantityLimit?: number;
  sold?: number;
}

export interface BundleProduct {
  productId: string;
  productName: string;
  productImage?: string;
  quantity: number;
  price: number;
}

// Promotion Models
export interface Promotion {
  id: string;
  name: string;
  description?: string;
  type: PromotionType;
  value: number;
  conditions: PromotionCondition[];
  actions: PromotionAction[];
  priority: number;
  isActive: boolean;
  validFrom: string;
  validUntil: string;
  usageLimit?: number;
  usageCount: number;
}

export interface PromotionCondition {
  type: 'MinPurchase' | 'MinQuantity' | 'SpecificProducts' | 'SpecificCategories' | 'CustomerTier' | 'FirstOrder';
  value: any;
}

export interface PromotionAction {
  type: 'PercentageDiscount' | 'FixedDiscount' | 'FreeShipping' | 'FreeProduct' | 'BuyXGetY';
  value: any;
}

export type PromotionType = 'CartLevel' | 'ProductLevel' | 'ShippingLevel';

// Tax Models
export interface TaxRate {
  id: string;
  name: string;
  rate: number;
  country: string;
  state?: string;
  postalCode?: string;
  priority: number;
  isCompound: boolean;
  isShippingTaxable: boolean;
  taxClass: string;
  isActive: boolean;
}

export interface TaxClass {
  id: string;
  name: string;
  description?: string;
  isDefault: boolean;
}

// Currency Models
export interface Currency {
  id: string;
  code: string;
  name: string;
  symbol: string;
  exchangeRate: number;
  isBaseCurrency: boolean;
  isActive: boolean;
  decimalPlaces: number;
  format: string;
}

// Payment Models
export interface PaymentMethod {
  id: string;
  customerId: string;
  type: 'Card' | 'BankAccount' | 'PayPal' | 'ApplePay' | 'GooglePay';
  last4: string;
  brand?: string;
  expiryMonth?: number;
  expiryYear?: number;
  isDefault: boolean;
  billingAddress?: Address;
}

export interface Payment {
  id: string;
  orderId: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  method: string;
  transactionId?: string;
  gatewayResponse?: string;
  createdAt: string;
  completedAt?: string;
}

// Content Models
export interface Page {
  id: string;
  title: string;
  slug: string;
  content: string;
  excerpt?: string;
  featuredImage?: string;
  seoTitle?: string;
  seoDescription?: string;
  isPublished: boolean;
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface BlogPost {
  id: string;
  title: string;
  slug: string;
  content: string;
  excerpt?: string;
  featuredImage?: string;
  authorId: string;
  authorName?: string;
  categoryId?: string;
  categoryName?: string;
  tags?: string[];
  seoTitle?: string;
  seoDescription?: string;
  isPublished: boolean;
  publishedAt?: string;
  viewCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface FAQ {
  id: string;
  question: string;
  answer: string;
  categoryId?: string;
  categoryName?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface Banner {
  id: string;
  title: string;
  subtitle?: string;
  image: string;
  mobileImage?: string;
  linkUrl?: string;
  linkText?: string;
  position: string;
  sortOrder: number;
  isActive: boolean;
  validFrom?: string;
  validUntil?: string;
}

// Support Models
export interface SupportTicket {
  id: string;
  ticketNumber: string;
  customerId: string;
  customerName?: string;
  customerEmail?: string;
  subject: string;
  category: string;
  priority: 'Low' | 'Medium' | 'High' | 'Urgent';
  status: TicketStatus;
  orderId?: string;
  assignedTo?: string;
  messages: TicketMessage[];
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
}

export interface TicketMessage {
  id: string;
  senderId: string;
  senderName: string;
  senderType: 'Customer' | 'Agent' | 'System';
  content: string;
  attachments?: string[];
  createdAt: string;
}

export type TicketStatus = 'Open' | 'InProgress' | 'WaitingOnCustomer' | 'WaitingOnAgent' | 'Resolved' | 'Closed';

// Affiliate Models
export interface Affiliate {
  id: string;
  customerId: string;
  code: string;
  referralUrl: string;
  commissionRate: number;
  totalEarnings: number;
  pendingEarnings: number;
  paidEarnings: number;
  totalReferrals: number;
  totalConversions: number;
  conversionRate: number;
  paymentMethod?: string;
  paymentDetails?: string;
  minPayout: number;
  isActive: boolean;
  createdAt: string;
}

export interface AffiliateReferral {
  id: string;
  affiliateId: string;
  visitorId?: string;
  customerId?: string;
  orderId?: string;
  commission?: number;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Paid';
  ipAddress?: string;
  referrer?: string;
  createdAt: string;
  convertedAt?: string;
}

// Analytics Models
export interface DashboardStats {
  totalRevenue: number;
  revenueGrowth: number;
  totalOrders: number;
  ordersGrowth: number;
  totalCustomers: number;
  customersGrowth: number;
  averageOrderValue: number;
  aovGrowth: number;
  conversionRate: number;
  conversionGrowth: number;
}

export interface SalesReport {
  period: string;
  revenue: number;
  orders: number;
  averageOrderValue: number;
  itemsSold: number;
}

export interface TopProduct {
  productId: string;
  productName: string;
  productImage?: string;
  quantity: number;
  revenue: number;
}

export interface TopCategory {
  categoryId: string;
  categoryName: string;
  productCount: number;
  revenue: number;
}

// Pagination
export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PageRequest {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  search?: string;
}

// API Response
export interface ApiResult<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}
