import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Aktivnost } from '../../models/aktivnost.model';
import { AktivnostService } from '../../services/aktivnost.service';
import { ZapisnikModalComponent } from '../zapisnik-modal/zapisnik-modal.component';
import { DevOpsTasksModalComponent } from '../devops-tasks-modal/devops-tasks-modal.component';
import { DevOpsTasksPreviewModalComponent, ParsedTask } from '../devops-tasks-preview-modal/devops-tasks-preview-modal.component';

@Component({
  selector: 'app-aktivnost-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ZapisnikModalComponent, DevOpsTasksModalComponent, DevOpsTasksPreviewModalComponent],
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

  zapisnik: string = '';
  isGeneratingZapisnik: boolean = false;
  showZapisnikModal: boolean = false;
  zapisnikModalTitle: string = '';
  showDevOpsTasksModal: boolean = false;
  showDevOpsTasksPreviewModal: boolean = false;
  parsedTasks: ParsedTask[] = [];

  constructor(private aktivnostService: AktivnostService) {}

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

  generateZapisnik(): void {
    if (!this.aktivnost.id || !this.aktivnost.detalji) {
      alert('Morate prvo sačuvati aktivnost i uneti detalje.');
      return;
    }

    this.zapisnikModalTitle = '📧 Generisani Zapisnik';
    this.isGeneratingZapisnik = true;
    this.showZapisnikModal = true;
    this.zapisnik = '';

    this.aktivnostService.generateZapisnik(this.aktivnost.id).subscribe({
      next: (response) => {
        this.zapisnik = response;
        this.isGeneratingZapisnik = false;
      },
      error: (error) => {
        console.error('Greška pri generisanju zapisnika:', error);
        alert('Greška prilikom generisanja zapisnika. Pokušajte ponovo.');
        this.isGeneratingZapisnik = false;
        this.showZapisnikModal = false;
      }
    });
  }

  closeZapisnikModal(): void {
    this.showZapisnikModal = false;
    this.zapisnik = '';
  }

  openDevOpsTasksModal(): void {
    this.showDevOpsTasksModal = true;
  }

  closeDevOpsTasksModal(): void {
    this.showDevOpsTasksModal = false;
  }

  generateDevOpsTasks(): void {
    if (!this.aktivnost.id || !this.aktivnost.detalji) {
      alert('Morate prvo sačuvati aktivnost i uneti detalje.');
      return;
    }

    this.zapisnikModalTitle = '📋 DevOps Taskovi';
    this.isGeneratingZapisnik = true;
    this.showZapisnikModal = true;
    this.zapisnik = '';

    this.aktivnostService.generateDevOpsTasks(this.aktivnost.id).subscribe({
      next: (response) => {
        this.zapisnik = response;
        this.isGeneratingZapisnik = false;
      },
      error: (error) => {
        console.error('Greška pri generisanju DevOps taskova:', error);
        alert('Greška prilikom generisanja taskova. Pokušajte ponovo.');
        this.isGeneratingZapisnik = false;
        this.showZapisnikModal = false;
      }
    });
  }

  openPreview(): void {
    if (!this.zapisnik || !this.aktivnost.id) return;

    this.aktivnostService.parseDevOpsTasks(this.aktivnost.id, this.zapisnik).subscribe({
      next: (tasks) => {
        this.parsedTasks = tasks.map(t => ({
          ...t,
          selected: true // Svi taskovi su default selektovani
        }));
        this.showZapisnikModal = false;
        this.showDevOpsTasksPreviewModal = true;
      },
      error: (error) => {
        console.error('Greška pri parsiranju taskova:', error);
        alert('Greška prilikom parsiranja taskova. Pokušajte ponovo.');
      }
    });
  }

  closePreviewModal(): void {
    this.showDevOpsTasksPreviewModal = false;
    this.parsedTasks = [];
  }

  saveSelectedTasks(tasks: ParsedTask[]): void {
    if (!this.aktivnost.id) return;

    const tasksToSave = tasks.map(t => ({
      title: t.title,
      description: t.description || '',
      acceptanceCriteria: t.acceptanceCriteria || '',
      priority: t.priority || 'Medium',
      estimation: t.estimation || '',
      orderIndex: t.orderIndex
    }));

    this.aktivnostService.saveSelectedTasks(this.aktivnost.id, tasksToSave).subscribe({
      next: (response) => {
        alert(`✅ Uspešno sačuvano ${response.count} taskova!`);
        this.closePreviewModal();
        this.zapisnik = '';
      },
      error: (error) => {
        console.error('Greška pri čuvanju taskova:', error);
        alert('Greška prilikom čuvanja taskova. Pokušajte ponovo.');
      }
    });
  }
}
