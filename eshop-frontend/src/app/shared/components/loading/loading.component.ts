import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './loading.component.html',
  styleUrl: './loading.component.css'
})
export class LoadingComponent {
  size = input<number>(40);
  message = input<string>('');
  fullScreen = input<boolean>(false);
  overlay = input<boolean>(false);
}
