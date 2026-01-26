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
    detalji: '',
    datum: new Date(),
    status: 'Planirana',
    vrsta: 'Razvoj',
    projekatId: 0
  };
  @Input() isOpen = false;
  @Output() save = new EventEmitter<Aktivnost>();
  @Output() close = new EventEmitter<void>();

  get datumString(): string {
    if (!this.aktivnost.datum) return '';
    const date = new Date(this.aktivnost.datum);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  set datumString(value: string) {
    this.aktivnost.datum = new Date(value);
  }

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
