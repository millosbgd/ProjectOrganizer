import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Aktivnost } from '../../models/aktivnost.model';

@Component({
  selector: 'app-aktivnost-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './aktivnost-modal.component.html',
  styleUrl: './aktivnost-modal.component.css'
})
export class AktivnostModalComponent {
  @Input() aktivnost: Aktivnost = {
    id: 0,
    opis: '',
    datum: new Date(),
    status: 'Planirana',
    vrsta: 'Razvoj',
    projekatId: 0
  };
  @Input() isOpen = false;
  @Output() save = new EventEmitter<Aktivnost>();
  @Output() close = new EventEmitter<void>();

  onSave(): void {
    this.save.emit(this.aktivnost);
  }

  onClose(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }
}
