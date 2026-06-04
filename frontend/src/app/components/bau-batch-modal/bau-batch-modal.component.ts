import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Klijent } from '../../models/klijent.model';
import { CodebookEntry } from '../../services/codebook.service';
import { AktivnostService, BauBatchCreateRow, BauBatchPreviewResult } from '../../services/aktivnost.service';

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
  isPreviewing = false;
  isGeneratingReportDescriptions = false;
  preview: BauBatchPreviewResult | null = null;

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
    this.clearPreview();
    this.rows.push({
      localId: this.nextLocalId++,
      klijentId: null,
      bauTipAktivnosti: '',
      trajanjeMinuta: 30,
      detalji: '',
      opisZaIzvestaj: ''
    });
  }

  removeRow(row: BauBatchGridRow): void {
    this.clearPreview();

    if (this.rows.length === 1) {
      this.rows = [];
      this.addRow();
      return;
    }

    this.rows = this.rows.filter(r => r.localId !== row.localId);
  }

  previewSchedule(): void {
    this.validationError = '';
    this.preview = null;

    const filledRows = this.getFilledRows();

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

    this.isPreviewing = true;
    this.aktivnostService.previewBauBatch({
      datum: this.toDateOnlyIso(this.datum),
      rows: filledRows
    }).subscribe({
      next: (preview) => {
        this.isPreviewing = false;
        this.preview = preview;
      },
      error: (error) => {
        this.isPreviewing = false;
        this.validationError = error?.error?.message || 'Greška pri raspodeli BAU aktivnosti.';
      }
    });
  }

  confirmSave(): void {
    this.validationError = '';

    if (!this.preview) {
      this.validationError = 'Prvo rasporedite BAU aktivnosti.';
      return;
    }

    const filledRows = this.getFilledRows();
    const previewByRowIndex = new Map(
      this.preview.items
        .filter(item => item.isBau && item.rowIndex !== null)
        .map(item => [item.rowIndex as number, item])
    );
    const rowsWithSchedule = filledRows.map((row, index) => {
      const previewItem = previewByRowIndex.get(index);
      return {
        ...row,
        opisZaIzvestaj: previewItem?.opisZaIzvestaj || row.opisZaIzvestaj || '',
        startUtc: previewItem?.startUtc,
        endUtc: previewItem?.endUtc
      };
    });

    if (rowsWithSchedule.some(row => !row.startUtc || !row.endUtc)) {
      this.validationError = 'Raspored više nije kompletan. Ponovo pokrenite raspodelu.';
      return;
    }

    this.isSaving = true;
    this.aktivnostService.createBauBatch({
      datum: this.toDateOnlyIso(this.datum),
      rows: rowsWithSchedule
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

  discardPreview(): void {
    this.clearPreview();
  }

  generateReportDescriptions(): void {
    this.validationError = '';

    if (!this.preview) {
      this.validationError = 'Prvo rasporedite BAU aktivnosti.';
      return;
    }

    const rows = this.preview.items
      .filter(item => item.isBau && item.rowIndex !== null)
      .map(item => ({
        rowIndex: item.rowIndex as number,
        klijentNaziv: item.klijentNaziv,
        bauTipAktivnostiNaziv: item.bauTipAktivnostiNaziv,
        detalji: item.detalji,
        scheduledDurationMinutes: item.scheduledDurationMinutes,
        startUtc: item.startUtc,
        endUtc: item.endUtc
      }));

    if (rows.length === 0) {
      this.validationError = 'Nema BAU aktivnosti za generisanje opisa.';
      return;
    }

    this.isGeneratingReportDescriptions = true;
    this.aktivnostService.generateBauBatchReportDescriptions(rows).subscribe({
      next: (result) => {
        const descriptions = new Map(result.items.map(item => [item.rowIndex, item.opisZaIzvestaj]));
        this.preview = {
          ...this.preview!,
          items: this.preview!.items.map(item => item.isBau && item.rowIndex !== null
            ? { ...item, opisZaIzvestaj: descriptions.get(item.rowIndex) || item.opisZaIzvestaj || '' }
            : item)
        };
        this.isGeneratingReportDescriptions = false;
      },
      error: (error) => {
        this.isGeneratingReportDescriptions = false;
        this.validationError = error?.error?.message || 'Greška pri generisanju opisa za izveštaj.';
      }
    });
  }

  updatePreviewReportDescription(item: any, value: string): void {
    if (!this.preview || !item.isBau || item.rowIndex === null) {
      return;
    }

    this.preview = {
      ...this.preview,
      items: this.preview.items.map(previewItem =>
        previewItem.isBau && previewItem.rowIndex === item.rowIndex
          ? { ...previewItem, opisZaIzvestaj: value }
          : previewItem)
    };
  }

  clearPreview(): void {
    this.preview = null;
  }

  formatTime(value: string): string {
    const date = new Date(value);
    return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`;
  }

  formatMinutes(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const remaining = minutes % 60;
    return hours > 0 ? `${hours}h ${String(remaining).padStart(2, '0')}m` : `${remaining}m`;
  }

  onClose(): void {
    this.reset();
    this.close.emit();
  }

  private getFilledRows(): BauBatchGridRow[] {
    return this.rows.filter(row =>
      !!row.klijentId || !!row.bauTipAktivnosti || !!row.detalji?.trim()
    );
  }

  private reset(): void {
    this.validationError = '';
    this.isSaving = false;
    this.isPreviewing = false;
    this.isGeneratingReportDescriptions = false;
    this.preview = null;
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
