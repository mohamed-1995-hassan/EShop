import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.css'
})
export class FooterComponent {
  email = '';
  currentYear = new Date().getFullYear();
  
  subscribe() {
    if (this.email) {
      console.log('Subscribe:', this.email);
      this.email = '';
      alert('Thank you for subscribing!');
    }
  }
}
