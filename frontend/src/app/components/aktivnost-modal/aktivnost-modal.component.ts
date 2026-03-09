import { Component, EventEmitter, Input, Output, ViewChild, ElementRef, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Aktivnost } from '../../models/aktivnost.model';
import { Projekat } from '../../models/projekat.model';
import { ProjectImplementationItem } from '../../models/project-implementation-item.model';
import { AktivnostService } from '../../services/aktivnost.service';
import { ProjekatService } from '../../services/projekat.service';
import { ProjectImplementationItemService } from '../../services/project-implementation-item.service';
import { OcrService } from '../../services/ocr.service';
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
export class AktivnostModalComponent implements OnChanges {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  
  @Input() aktivnost: Aktivnost = {
    id: 0,
    opis: '',
    detalji: '',
    datum: new Date(),
    status: 'Planirana',
    vrsta: 'Razvoj',
    bau: false,
    projekatId: 0
  };
  @Input() isOpen = false;
  @Input() isProjectLocked = false; // When true, projekatId cannot be changed
  @Output() save = new EventEmitter<Aktivnost>();
  @Output() close = new EventEmitter<void>();
  @Output() delete = new EventEmitter<number>();
  @Output() refreshNeeded = new EventEmitter<void>();

  projekti: Projekat[] = [];
  implementationItems: ProjectImplementationItem[] = [];
  zapisnik: string = '';
  isGeneratingZapisnik: boolean = false;
  showZapisnikModal: boolean = false;
  zapisnikModalTitle: string = '';
  showDevOpsTasksModal: boolean = false;
  showDevOpsTasksPreviewModal: boolean = false;
  parsedTasks: ParsedTask[] = [];
  validationError: string = ''; // Validation error message
  showSuccessMessage: boolean = false;
  isSaving: boolean = false;

  // Time fields
  readonly DURATION_STEP_MINUTES = 15; // 15 minutes per click

  // OCR properties
  selectedImageFile: File | null = null;
  selectedImagePreview: string | null = null;
  isExtractingText: boolean = false;

  constructor(
    private aktivnostService: AktivnostService,
    private projekatService: ProjekatService,
    private implementationItemService: ProjectImplementationItemService,
    private ocrService: OcrService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.loadProjekti();
      if (this.aktivnost.projekatId && this.aktivnost.projekatId > 0) {
        this.loadImplementationItems();
      }
      // Set default times for new aktivnost (current time + 1 hour)
      if (!this.aktivnost.id || this.aktivnost.id === 0) {
        this.setDefaultTimes();
      }
    }
    if (changes['aktivnost'] && this.aktivnost.projekatId && this.aktivnost.projekatId > 0) {
      this.loadImplementationItems();
    }
  }

  setDefaultTimes(): void {
    // Only set default times if they're not already set
    if (this.aktivnost.startUtc && this.aktivnost.endUtc) {
      return;
    }

    const now = new Date();
    // Round to next 15 minutes
    const minutes = now.getMinutes();
    const roundedMinutes = Math.ceil(minutes / 15) * 15;
    now.setMinutes(roundedMinutes);
    now.setSeconds(0);
    now.setMilliseconds(0);

    // Set start time to rounded current time
    this.aktivnost.startUtc = now.toISOString();

    // Set end time to 1 hour later
    const endTime = new Date(now.getTime() + 60 * 60000); // +1 hour
    this.aktivnost.endUtc = endTime.toISOString();
  }

  loadProjekti(): void {
    // Load only user's projects (createdByMe=true)
    this.projekatService.getAll(true).subscribe({
      next: (data) => {
        this.projekti = data;
      },
      error: (error) => {
        console.error('Error loading projects:', error);
      }
    });
  }

  onBauChange(): void {
    if (this.aktivnost.bau) {
      this.aktivnost.projekatId = null;
      this.aktivnost.projectImplementationItemId = undefined;
      this.implementationItems = [];
    } else {
      this.aktivnost.projekatId = 0;
    }
  }

  onProjekatChange(): void {
    if (this.aktivnost.projekatId && this.aktivnost.projekatId > 0) {
      this.loadImplementationItems();
    } else {
      this.implementationItems = [];
      this.aktivnost.projectImplementationItemId = undefined;
    }
  }

  getProjektNaziv(): string {
    const projekat = this.projekti.find(p => p.id === this.aktivnost.projekatId);
    return projekat ? `${projekat.brojProjekta} - ${projekat.naziv}` : '';
  }

  loadImplementationItems(): void {
    if (this.aktivnost.projekatId == null || this.aktivnost.projekatId <= 0) {
      return;
    }
    this.implementationItemService.getByProjectId(this.aktivnost.projekatId).subscribe({
      next: (data) => {
        this.implementationItems = data;
      },
      error: (error) => {
        console.error('Error loading implementation items:', error);
      }
    });
  }

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

  // Start time in HH:mm format (local timezone)
  get startTime(): string {
    if (!this.aktivnost.startUtc) return '';
    const date = new Date(this.aktivnost.startUtc);
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${hours}:${minutes}`;
  }

  set startTime(value: string) {
    if (!value) {
      this.aktivnost.startUtc = undefined;
      return;
    }
    // Get current datum value for combining with time
    const currentDatum = this.datumString || new Date().toISOString().split('T')[0];
    const localDateTime = `${currentDatum}T${value}:00`;
    const resultDate = new Date(localDateTime);
    if (!isNaN(resultDate.getTime())) {
      this.aktivnost.startUtc = resultDate.toISOString();
      this.validationError = ''; // Clear validation error when time is set
    }
  }

  // End time in HH:mm format (local timezone)
  get endTime(): string {
    if (!this.aktivnost.endUtc) return '';
    const date = new Date(this.aktivnost.endUtc);
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${hours}:${minutes}`;
  }

  set endTime(value: string) {
    if (!value) {
      this.aktivnost.endUtc = undefined;
      return;
    }
    // Get current datum value for combining with time
    const currentDatum = this.datumString || new Date().toISOString().split('T')[0];
    const localDateTime = `${currentDatum}T${value}:00`;
    const resultDate = new Date(localDateTime);
    if (!isNaN(resultDate.getTime())) {
      this.aktivnost.endUtc = resultDate.toISOString();
      this.validationError = ''; // Clear validation error when time is set
    }
  }

  // Calculate current duration in minutes
  getDurationMinutes(): number {
    if (!this.aktivnost.startUtc || !this.aktivnost.endUtc) {
      return 0;
    }
    const start = new Date(this.aktivnost.startUtc);
    const end = new Date(this.aktivnost.endUtc);
    const diffMs = end.getTime() - start.getTime();
    return Math.round(diffMs / 60000); // Convert ms to minutes
  }

  // Get duration label for display
  getDurationLabel(): string {
    const minutes = this.getDurationMinutes();
    if (minutes === 0) {
      return '0 min';
    }
    if (minutes < 60) {
      return `${minutes} min`;
    }
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    if (remainingMinutes === 0) {
      return `${hours} ${hours === 1 ? 'sat' : 'sati'}`;
    }
    return `${hours}h ${remainingMinutes}min`;
  }

  // Increment duration by DURATION_STEP_MINUTES
  incrementDuration(): void {
    this.validationError = ''; // Clear validation error
    
    if (!this.aktivnost.startUtc) {
      // If no start time, set default start time to now
      const now = new Date();
      const currentDatum = this.datumString || now.toISOString().split('T')[0];
      const localDateTime = `${currentDatum}T${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:00`;
      this.aktivnost.startUtc = new Date(localDateTime).toISOString();
    }

    if (!this.aktivnost.endUtc) {
      // If no end time, set it to start + DURATION_STEP_MINUTES
      const startDate = new Date(this.aktivnost.startUtc);
      const endDate = new Date(startDate.getTime() + this.DURATION_STEP_MINUTES * 60000);
      this.aktivnost.endUtc = endDate.toISOString();
    } else {
      // Add DURATION_STEP_MINUTES to end time
      const endDate = new Date(this.aktivnost.endUtc);
      endDate.setMinutes(endDate.getMinutes() + this.DURATION_STEP_MINUTES);
      this.aktivnost.endUtc = endDate.toISOString();
    }
  }

  // Decrement duration by DURATION_STEP_MINUTES
  decrementDuration(): void {
    this.validationError = ''; // Clear validation error
    
    if (!this.aktivnost.startUtc || !this.aktivnost.endUtc) {
      return;
    }

    const currentDuration = this.getDurationMinutes();
    if (currentDuration <= this.DURATION_STEP_MINUTES) {
      // Don't go below DURATION_STEP_MINUTES
      return;
    }

    // Subtract DURATION_STEP_MINUTES from end time
    const endDate = new Date(this.aktivnost.endUtc);
    endDate.setMinutes(endDate.getMinutes() - this.DURATION_STEP_MINUTES);
    this.aktivnost.endUtc = endDate.toISOString();
  }

  onSave(): void {
    // Clear previous validation error
    this.validationError = '';

    // Validate required fields
    if (!this.aktivnost.startUtc || !this.aktivnost.endUtc) {
      this.validationError = '⚠️ Morate uneti vreme početka i završetka aktivnosti.';
      return;
    }

    // Validate end time is after start time
    const start = new Date(this.aktivnost.startUtc);
    const end = new Date(this.aktivnost.endUtc);
    if (end <= start) {
      this.validationError = '⚠️ Vreme završetka mora biti nakon vremena početka.';
      return;
    }

    this.isSaving = true;

    if (this.aktivnost.id && this.aktivnost.id > 0) {
      // Update existing
      this.aktivnostService.update(this.aktivnost.id, this.aktivnost).subscribe({
        next: () => {
          this.isSaving = false;
          this.showSuccessMessageAndRefresh();
        },
        error: (error) => {
          this.isSaving = false;
          console.error('Error updating aktivnost:', error);
          this.validationError = '⚠️ Greška prilikom ažuriranja aktivnosti.';
        }
      });
    } else {
      // Create new
      this.aktivnostService.create(this.aktivnost).subscribe({
        next: (created) => {
          this.isSaving = false;
          this.aktivnost = created;
          this.showSuccessMessageAndRefresh();
        },
        error: (error) => {
          this.isSaving = false;
          console.error('Error creating aktivnost:', error);
          this.validationError = '⚠️ Greška prilikom kreiranja aktivnosti.';
        }
      });
    }
  }

  showSuccessMessageAndRefresh(): void {
    this.showSuccessMessage = true;
    
    // Hide success message after 3 seconds
    setTimeout(() => {
      this.showSuccessMessage = false;
    }, 3000);

    // Notify parent to refresh the list
    this.refreshNeeded.emit();
  }

  onClose(): void {
    this.validationError = ''; // Clear validation error on close
    this.close.emit();
  }

  onDelete(): void {
    if (!this.aktivnost.id || this.aktivnost.id <= 0) {
      return;
    }

    const confirmed = confirm('Da li ste sigurni da želite da obrišete aktivnost?');
    if (!confirmed) {
      return;
    }

    this.aktivnostService.delete(this.aktivnost.id).subscribe({
      next: () => {
        this.delete.emit(this.aktivnost.id);
        this.onClose();
      },
      error: (error) => {
        if (error.error && error.error.message) {
          alert(error.error.message);
        } else {
          alert('Greška prilikom brisanja aktivnosti.');
        }
      }
    });
  }

  onBackdropClick(event: MouseEvent): void {
    // Modal se zatvara samo eksplicitno preko dugmeta Close ili Cancel
    // Ne dozvoljavamo zatvaranje klikom na backdrop
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

  // OCR Methods
  triggerFileInput(): void {
    this.fileInput.nativeElement.click();
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      
      // Validate file type
      if (!file.type.startsWith('image/')) {
        alert('Molimo izaberite sliku (PNG, JPG, itd.)');
        return;
      }

      // Validate file size (max 10MB)
      if (file.size > 10 * 1024 * 1024) {
        alert('Slika je prevelika. Maksimalna veličina je 10MB.');
        return;
      }

      this.selectedImageFile = file;

      // Create preview
      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        this.selectedImagePreview = e.target?.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  removeImage(): void {
    this.selectedImageFile = null;
    this.selectedImagePreview = null;
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
  }

  extractTextFromImage(): void {
    if (!this.selectedImageFile || !this.selectedImagePreview) {
      return;
    }

    this.isExtractingText = true;

    // Extract base64 without data:image/...;base64, prefix
    const base64Data = this.selectedImagePreview.split(',')[1];

    this.ocrService.extractText(base64Data).subscribe({
      next: (response) => {
        // Append extracted text to existing detalji
        if (this.aktivnost.detalji) {
          this.aktivnost.detalji += '\n\n' + response.text;
        } else {
          this.aktivnost.detalji = response.text;
        }
        
        this.isExtractingText = false;
        this.removeImage(); // Clean up after successful extraction
        
        alert('✅ Tekst uspešno očitan i dodat u detalje!');
      },
      error: (error) => {
        console.error('Greška pri očitavanju teksta:', error);
        this.isExtractingText = false;
        alert('Greška prilikom očitavanja teksta sa slike. Pokušajte ponovo.');
      }
    });
  }
}
