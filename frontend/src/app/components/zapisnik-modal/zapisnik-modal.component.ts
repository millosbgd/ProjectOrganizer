import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-zapisnik-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './zapisnik-modal.component.html',
  styleUrl: './zapisnik-modal.component.css'
})
export class ZapisnikModalComponent {
  @Input() zapisnik: string = '';
  @Input() isGenerating: boolean = false;
  @Output() close = new EventEmitter<void>();

  copyToClipboard() {
    navigator.clipboard.writeText(this.zapisnik).then(() => {
      alert('Zapisnik je kopiran u clipboard!');
    });
  }

  closeModal() {
    this.close.emit();
  }
}
