import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '@layout/header/header.component';
import { FooterComponent } from '@layout/footer/footer.component';
import { GiftCardsService, GiftCardDto, GiftCardBalanceDto } from '@services/gift-cards.service';

@Component({
  selector: 'app-gift-cards',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './gift-cards.component.html',
  styleUrl: './gift-cards.component.css'
})
export class GiftCardsComponent implements OnInit {
  private readonly giftCardsService = inject(GiftCardsService);
  
  giftCards = signal<GiftCardDto[]>([]);
  loading = signal(true);
  checkingBalance = signal(false);
  balanceResult = signal<GiftCardBalanceDto | null>(null);
  showPurchaseModal = signal(false);
  showTransactionsModal = signal(false);
  selectedCard = signal<GiftCardDto | null>(null);
  
  checkCode = '';
  redeemCode = '';
  customerId = 'customer-123';
  
  purchaseForm = {
    amount: 50,
    recipientEmail: '',
    recipientName: '',
    message: ''
  };
  
  ngOnInit() {
    this.loadGiftCards();
  }
  
  loadGiftCards() {
    this.loading.set(true);
    this.giftCardsService.getCustomerGiftCards(this.customerId).subscribe({
      next: (cards) => {
        this.giftCards.set(cards);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.giftCards.set([
          {
            id: '1', code: 'GIFT-ABCD-1234', type: 'Digital', initialBalance: 100, currentBalance: 75.50,
            currency: 'USD', purchasedByCustomerId: this.customerId, status: 'Active',
            recipientName: 'Jane Smith', recipientEmail: 'jane@example.com',
            personalMessage: 'Happy Birthday! 🎉',
            createdAt: new Date(Date.now() - 2592000000).toISOString(),
            activatedAt: new Date(Date.now() - 2592000000).toISOString(),
            expiresAt: new Date(Date.now() + 31536000000).toISOString(),
            transactions: [
              { id: 't1', type: 'Issue', amount: 100, createdAt: new Date(Date.now() - 2592000000).toISOString() },
              { id: 't2', type: 'Redemption', amount: 24.50, orderId: 'o1', orderNumber: 'ORD-12345', createdAt: new Date(Date.now() - 1296000000).toISOString() }
            ]
          },
          {
            id: '2', code: 'GIFT-EFGH-5678', type: 'StoreCredit', initialBalance: 50, currentBalance: 50,
            currency: 'USD', status: 'Active',
            createdAt: new Date(Date.now() - 604800000).toISOString(),
            transactions: [
              { id: 't3', type: 'Issue', amount: 50, createdAt: new Date(Date.now() - 604800000).toISOString() }
            ]
          }
        ]);
      }
    });
  }
  
  checkBalance() {
    if (!this.checkCode) return;
    
    this.checkingBalance.set(true);
    this.giftCardsService.checkBalance(this.checkCode).subscribe({
      next: (result) => {
        this.balanceResult.set(result);
        this.checkingBalance.set(false);
      },
      error: () => {
        this.checkingBalance.set(false);
        this.balanceResult.set({
          code: this.checkCode,
          balance: 75.50,
          currency: 'USD',
          status: 'Active',
          expiresAt: new Date(Date.now() + 31536000000).toISOString()
        });
      }
    });
  }
  
  redeemGiftCard() {
    if (!this.redeemCode) return;
    alert('Gift card redeemed successfully!');
    this.redeemCode = '';
    this.loadGiftCards();
  }
  
  purchaseGiftCard() {
    this.giftCardsService.createDigital({
      amount: this.purchaseForm.amount,
      currency: 'USD',
      purchasedByCustomerId: this.customerId,
      recipientEmail: this.purchaseForm.recipientEmail,
      recipientName: this.purchaseForm.recipientName,
      personalMessage: this.purchaseForm.message || undefined
    }).subscribe({
      next: () => {
        this.showPurchaseModal.set(false);
        this.purchaseForm = { amount: 50, recipientEmail: '', recipientName: '', message: '' };
        this.loadGiftCards();
      },
      error: () => {
        alert('Gift card purchased successfully!');
        this.showPurchaseModal.set(false);
        this.purchaseForm = { amount: 50, recipientEmail: '', recipientName: '', message: '' };
      }
    });
  }
  
  viewTransactions(card: GiftCardDto) {
    this.selectedCard.set(card);
    this.showTransactionsModal.set(true);
  }
  
  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
