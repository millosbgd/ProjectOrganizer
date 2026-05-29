import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Klijent } from '../../models/klijent.model';
import { CodebookEntry } from '../../services/codebook.service';
import { AktivnostService, BauBatchCreateRow } from '../../services/aktivnost.service';

interface BauBatchGridRow extends BauBatchCreateRow {
  localId: number;
}

@Component({
  selector: 'app-bau-batch-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bau-batch-modal.component.html',
  styleUrl: './bau-batch-modal.component.css'
})
export class BauBatchModalComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() datum: Date = new Date();
  @Input() klijenti: Klijent[] = [];
  @Input() bauTypes: CodebookEntry[] = [];
  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  rows: BauBatchGridRow[] = [];
  durationOptions = [15, 30, 45, 60, 90];
  validationError = '';
  isSaving = false;

  private nextLocalId = 1;

  constructor(private aktivnostService: AktivnostService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen && this.rows.length === 0) {
      this.addRow();
      this.addRow();
      this.addRow();
    }
  }

  addRow(): void {
    this.rows.push({
      localId: this.nextLocalId++,
      klijentId: null,
      bauTipAktivnosti: '',
      trajanjeMinuta: 30,
      detalji: ''
    });
  }

  removeRow(row: BauBatchGridRow): void {
    if (this.rows.length === 1) {
      this.rows = [];
      this.addRow();
      return;
    }

    this.rows = this.rows.filter(r => r.localId !== row.localId);
  }

  save(): void {
    this.validationError = '';

    const filledRows = this.rows.filter(row =>
      !!row.klijentId || !!row.bauTipAktivnosti || !!row.trajanjeMinuta || !!row.detalji?.trim()
    );

    if (filledRows.length === 0) {
      this.validationError = 'Unesite najmanje jednu BAU aktivnost.';
      return;
    }

    const invalidIndex = filledRows.findIndex(row =>
      !row.klijentId || !row.bauTipAktivnosti || !row.trajanjeMinuta
    );

    if (invalidIndex >= 0) {
      this.validationError = `Red ${invalidIndex + 1}: klijent, tip aktivnosti i trajanje su obavezni.`;
      return;
    }

    this.isSaving = true;
    this.aktivnostService.createBauBatch({
      datum: this.toDateOnlyIso(this.datum),
      rows: filledRows
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.reset();
        this.saved.emit();
      },
      error: (error) => {
        this.isSaving = false;
        this.validationError = error?.error?.message || 'Greška pri čuvanju BAU aktivnosti.';
      }
    });
  }

  onClose(): void {
    this.reset();
    this.close.emit();
  }

  private reset(): void {
    this.validationError = '';
    this.isSaving = false;
    this.rows = [];
    this.nextLocalId = 1;
  }

  private toDateOnlyIso(date: Date): string {
    const value = new Date(date);
    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}T00:00:00`;
  }
}
