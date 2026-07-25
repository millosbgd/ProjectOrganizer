import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SablonPodrske } from '../../models/sablon-podrske.model';
import { Klijent } from '../../models/klijent.model';
import { KlijentService } from '../../services/klijent.service';
import { Dokument } from '../../models/dokument.model';
import { DokumentService } from '../../services/dokument.service';

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
  dokumenti: Dokument[] = [];
  validationError = '';
  isSaving = false;
  showDokumentiSidebar = false;
  uploadingFile = false;

  constructor(
    private klijentService: KlijentService,
    private dokumentService: DokumentService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.validationError = '';
      this.isSaving = false;
      this.loadKlijenti();
      if (this.sablon.id && this.sablon.id > 0) {
        this.loadDokumenti();
      } else {
        this.dokumenti = [];
        this.showDokumentiSidebar = false;
      }
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
    this.showDokumentiSidebar = false;
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

  loadDokumenti(): void {
    if (!this.sablon.id || this.sablon.id <= 0) {
      return;
    }

    this.dokumentService.getByEntity('SablonPodrske', this.sablon.id).subscribe({
      next: (data) => {
        this.dokumenti = data;
      },
      error: (error) => {
        console.error('Error loading dokumenti:', error);
      }
    });
  }

  toggleDokumentiSidebar(): void {
    if (!this.sablon.id || this.sablon.id <= 0) {
      return;
    }

    this.showDokumentiSidebar = !this.showDokumentiSidebar;
    if (this.showDokumentiSidebar) {
      this.loadDokumenti();
    }
  }

  onDokumentSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.uploadDokument(file);
      input.value = '';
    }
  }

  uploadDokument(file: File): void {
    if (!this.sablon.id || this.sablon.id <= 0) {
      return;
    }

    this.uploadingFile = true;
    this.dokumentService.uploadForEntity('SablonPodrske', this.sablon.id, file).subscribe({
      next: () => {
        this.loadDokumenti();
        this.uploadingFile = false;
      },
      error: (error) => {
        console.error('Error uploading file:', error);
        alert('Greška pri upload-u fajla');
        this.uploadingFile = false;
      }
    });
  }

  downloadDokument(dokument: Dokument): void {
    this.dokumentService.downloadDokument(dokument.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = dokument.nazivFajla;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (error) => {
        console.error('Error downloading file:', error);
        alert('Greška pri preuzimanju fajla');
      }
    });
  }

  deleteDokument(id: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete ovaj dokument?')) {
      this.dokumentService.deleteDokument(id).subscribe({
        next: () => {
          this.loadDokumenti();
        },
        error: (error) => {
          console.error('Error deleting dokument:', error);
          alert('Greška pri brisanju dokumenta');
        }
      });
    }
  }

  formatFileSize(bytes: number): string {
    return this.dokumentService.formatFileSize(bytes);
  }

  getFileIcon(tipFajla: string): string {
    const icons: { [key: string]: string } = {
      'pdf': '📄',
      'xls': '📊',
      'xlsx': '📊',
      'eml': '✉️',
      'doc': '📝',
      'docx': '📝',
      'txt': '📝',
      'md': '📝'
    };
    return icons[tipFajla.toLowerCase()] || '📄';
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
