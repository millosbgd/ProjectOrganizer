import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SablonPodrske } from '../../models/sablon-podrske.model';
import { Klijent } from '../../models/klijent.model';
import { KlijentService } from '../../services/klijent.service';

@Component({
  selector: 'app-sablon-podrske-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sablon-podrske-modal.component.html',
  styleUrl: './sablon-podrske-modal.component.css'
})
export class SablonPodrskeModalComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() sablon: SablonPodrske = this.getEmptySablon();
  @Output() save = new EventEmitter<SablonPodrske>();
  @Output() close = new EventEmitter<void>();
  @Output() delete = new EventEmitter<number>();

  klijenti: Klijent[] = [];
  validationError = '';
  isSaving = false;

  constructor(private klijentService: KlijentService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.validationError = '';
      this.isSaving = false;
      this.loadKlijenti();
    }
  }

  onSave(): void {
    this.validationError = '';

    if (!this.sablon.naslov?.trim()) {
      this.validationError = 'Naslov je obavezan.';
      return;
    }

    if (!this.sablon.opisResenja?.trim()) {
      this.validationError = 'Opis rešenja je obavezan.';
      return;
    }

    this.save.emit({
      ...this.sablon,
      klijentId: this.sablon.klijentId || null,
      naslov: this.sablon.naslov.trim(),
      opisZahteva: this.sablon.opisZahteva?.trim() || '',
      opisResenja: this.sablon.opisResenja.trim(),
      odgovorKlijentu: this.sablon.odgovorKlijentu?.trim() || ''
    });
  }

  onClose(): void {
    this.validationError = '';
    this.close.emit();
  }

  onDelete(): void {
    if (!this.sablon.id || this.sablon.id <= 0) {
      return;
    }

    if (confirm('Da li ste sigurni da želite da obrišete šablon za podršku?')) {
      this.delete.emit(this.sablon.id);
    }
  }

  onBackdropClick(event: MouseEvent): void {
  }

  private loadKlijenti(): void {
    this.klijentService.getAll().subscribe({
      next: (data) => {
        this.klijenti = data;
      },
      error: (error) => {
        console.error('Error loading clients:', error);
      }
    });
  }

  private getEmptySablon(): SablonPodrske {
    return {
      id: 0,
      klijentId: null,
      naslov: '',
      opisZahteva: '',
      opisResenja: '',
      odgovorKlijentu: ''
    };
  }
}
