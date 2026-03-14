import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProjekatService } from '../../services/projekat.service';
import { KlijentService } from '../../services/klijent.service';
import { AktivnostService } from '../../services/aktivnost.service';
import { DokumentService } from '../../services/dokument.service';
import { NoteService } from '../../services/note.service';
import { ImplementationModelService } from '../../services/implementation-model.service';
import { ProjectImplementationItemService } from '../../services/project-implementation-item.service';
import { Projekat } from '../../models/projekat.model';
import { Klijent } from '../../models/klijent.model';
import { Aktivnost } from '../../models/aktivnost.model';
import { Dokument } from '../../models/dokument.model';
import { Note } from '../../models/note.model';
import { ImplementationModel } from '../../models/implementation-model.model';
import { ProjectImplementationItem } from '../../models/project-implementation-item.model';
import { AktivnostModalComponent } from '../aktivnost-modal/aktivnost-modal.component';
import { ImplementationItemModalComponent } from '../implementation-item-modal/implementation-item-modal.component';

@Component({
  selector: 'app-projekat-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AktivnostModalComponent, ImplementationItemModalComponent],
  templateUrl: './projekat-detail.component.html',
  styleUrls: ['./projekat-detail.component.css']
})
export class ProjekatDetailComponent implements OnInit {
  projekat: Projekat = {
    id: 0,
    brojProjekta: '',
    datum: new Date(),
    naziv: '',
    aktivan: true,
    aiPracen: false,
    status: 'U pripremi',
    klijentId: 0
  };
  
  klijenti: Klijent[] = [];
  implementationModels: ImplementationModel[] = [];
  aktivnosti: Aktivnost[] = [];
  implementationItems: ProjectImplementationItem[] = [];
  dokumenti: Dokument[] = [];
  notes: Note[] = [];
  isEditMode = false;
  isNewMode = false;
  loading = true;
  uploadingFile = false;
  activeTab: 'aktivnosti' | 'implementacija' = 'aktivnosti';
  
  currentAktivnost: Aktivnost = {
    id: 0,
    opis: '',
    detalji: '',
    datum: new Date(),
    status: 'Planirana',
    vrsta: 'Razvoj',
    projekatId: 0
  };
  
  showAktivnostModal = false;
  showImplementationItemModal = false;
  currentImplementationItem: ProjectImplementationItem | null = null;
  openDropdownId: number | null = null;
  showDokumentiSidebar = false;
  showNotesSidebar = false;
  currentNote: string = '';
  editingNoteId: number | null = null;

  constructor(
    private projekatService: ProjekatService,
    private noteService: NoteService,
    private dokumentService: DokumentService,
    private klijentService: KlijentService,
    private aktivnostService: AktivnostService,
    private implementationModelService: ImplementationModelService,
    private implementationItemService: ProjectImplementationItemService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadKlijenti();
    this.loadImplementationModels();
    
    const id = this.route.snapshot.paramMap.get('id');
    
    if (id === 'new') {
      this.isNewMode = true;
      this.isEditMode = true;
      this.loading = false;
      this.loadNotes(+id);
    } else if (id) {
      this.loadProjekat(+id);
      this.loadAktivnosti(+id);
      this.loadImplementationItems(+id);
      this.loadDokumenti(+id);
      this.loadNotes(+id);
    }
  }

  setActiveTab(tab: 'aktivnosti' | 'implementacija'): void {
    this.activeTab = tab;
  }

  loadKlijenti(): void {
    this.klijentService.getAll().subscribe({
      next: (data) => {
        this.klijenti = data;
      },
      error: (error) => {
        console.error('Error loading klijenti:', error);
      }
    });
  }

  loadImplementationModels(): void {
    this.implementationModelService.getAll().subscribe({
      next: (data) => {
        this.implementationModels = data;
      },
      error: (error) => {
        console.error('Error loading implementation models:', error);
      }
    });
  }

  loadProjekat(id: number): void {
    this.projekatService.getById(id).subscribe({
      next: (data) => {
        this.projekat = data;
        // Convert datum to YYYY-MM-DD format for date input
        if (this.projekat.datum) {
          const date = new Date(this.projekat.datum);
          this.projekat.datum = date.toISOString().split('T')[0];
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading projekat:', error);
        this.loading = false;
      }
    });
  }

  loadAktivnosti(projekatId: number): void {
    this.aktivnostService.getByProjekatId(projekatId).subscribe({
      next: (data) => {
        this.aktivnosti = data;
      },
      error: (error) => {
        console.error('Error loading aktivnosti:', error);
      }
    });
  }

  loadImplementationItems(projekatId: number): void {
    this.implementationItemService.getByProjectId(projekatId).subscribe({
      next: (data) => {
        this.implementationItems = data;
      },
      error: (error) => {
        console.error('Error loading implementation items:', error);
      }
    });
  }

  openImplementationItemModal(item: ProjectImplementationItem): void {
    // Load full details including checklists
    this.implementationItemService.getById(item.id).subscribe({
      next: (fullItem) => {
        this.currentImplementationItem = fullItem;
        this.showImplementationItemModal = true;
      },
      error: (error) => {
        console.error('Error loading implementation item details:', error);
        alert('Greška pri učitavanju detalja stavke.');
      }
    });
  }

  closeImplementationItemModal(): void {
    this.showImplementationItemModal = false;
    this.currentImplementationItem = null;
  }

  getTotalProcent(item: ProjectImplementationItem): number {
    if (!item.checkLists || item.checkLists.length === 0) {
      return 0;
    }
    return item.checkLists.reduce((sum, cl) => sum + (cl.procenat || 0), 0);
  }

  getCompletedProcent(item: ProjectImplementationItem): number {
    if (!item.checkLists || item.checkLists.length === 0) {
      return 0;
    }
    return item.checkLists
      .filter(cl => cl.zavrsen)
      .reduce((sum, cl) => sum + (cl.procenat || 0), 0);
  }

  getTotalProcentColor(item: ProjectImplementationItem): string {
    const total = this.getTotalProcent(item);
    if (total === 0) return '#999'; // Gray for no data
    if (Math.abs(total - 100) < 0.01) return '#28a745'; // Green for 100%
    return '#333'; // Dark gray for not 100% (changed from red)
  }

  getCompletedProcentColor(item: ProjectImplementationItem): string {
    const completed = this.getCompletedProcent(item);
    if (completed === 0) return '#999'; // Gray for no data
    return '#28a745'; // Green for completed
  }

  saveImplementationItem(item: ProjectImplementationItem): void {
    this.implementationItemService.update(item.id, item).subscribe({
      next: () => {
        this.closeImplementationItemModal();
        this.loadImplementationItems(this.projekat.id);
      },
      error: (error) => {
        console.error('Error updating implementation item:', error);
        alert('Greška pri ažuriranju stavke');
      }
    });
  }

  saveProjekat(): void {
    // Convert date to YYYY-MM-DD format for DateOnly on backend
    let datumForServer: string;
    if (this.projekat.datum) {
      if (typeof this.projekat.datum === 'string') {
        // Input date is already in YYYY-MM-DD format
        datumForServer = this.projekat.datum;
      } else {
        // Convert Date object to YYYY-MM-DD
        const date = new Date(this.projekat.datum);
        datumForServer = date.toISOString().split('T')[0];
      }
    } else {
      datumForServer = new Date().toISOString().split('T')[0];
    }

    if (this.isNewMode) {
      // For new projects, don't send brojProjekta at all
      const projekatToSave = {
        naziv: this.projekat.naziv,
        datum: datumForServer,
        aktivan: this.projekat.aktivan,
        aiPracen: this.projekat.aiPracen ?? false,
        status: this.projekat.status,
        klijentId: this.projekat.klijentId,
        implementationModelId: this.projekat.implementationModelId
      };

      this.projekatService.create(projekatToSave as any).subscribe({
        next: (data) => {
          this.router.navigate(['/projekti']);
        },
        error: (error) => {
          console.error('Error creating projekat:', error);
          console.error('Error details:', error.error);
          const errorMsg = typeof error.error === 'string' 
            ? error.error 
            : JSON.stringify(error.error);
          alert('Greška pri kreiranju projekta: ' + errorMsg);
        }
      });
    } else {
      // For updates, include brojProjekta
      const projekatToSave = {
        id: this.projekat.id,
        brojProjekta: this.projekat.brojProjekta,
        datum: datumForServer,
        naziv: this.projekat.naziv,
        aktivan: this.projekat.aktivan,
        aiPracen: this.projekat.aiPracen ?? false,
        status: this.projekat.status,
        klijentId: this.projekat.klijentId,
        implementationModelId: this.projekat.implementationModelId
      };

      this.projekatService.update(this.projekat.id, projekatToSave).subscribe({
        next: () => {
          this.isEditMode = false;
          this.loadProjekat(this.projekat.id);
        },
        error: (error) => {
          console.error('Error updating projekat:', error);
          console.error('Error details:', error.error);
          let errorMsg = 'Nepoznata greška';
          if (error.error) {
            if (typeof error.error === 'string') {
              errorMsg = error.error;
            } else if (error.error.message) {
              errorMsg = error.error.message;
            } else if (error.error.title) {
              errorMsg = error.error.title;
            } else {
              errorMsg = JSON.stringify(error.error);
            }
          } else if (error.message) {
            errorMsg = error.message;
          }
          alert('Greška pri ažuriranju projekta: ' + errorMsg);
        }
      });
    }
  }

  cancel(): void {
    if (this.isNewMode) {
      this.router.navigate(['/projekti']);
    } else {
      this.isEditMode = false;
      this.loadProjekat(this.projekat.id);
    }
  }

  addAktivnost(): void {
    this.currentAktivnost.projekatId = this.projekat.id;
    this.aktivnostService.create(this.currentAktivnost).subscribe({
      next: () => {
        this.loadAktivnosti(this.projekat.id);
        this.closeAktivnostModal();
      },
      error: (error) => {
        console.error('Error creating aktivnost:', error);
      }
    });
  }

  openNewAktivnostModal(): void {
    this.currentAktivnost = {
      id: 0,
      opis: '',
      detalji: '',
      datum: new Date(),
      status: 'Planirana',
      vrsta: 'Razvoj',
      projekatId: this.projekat.id
    };
    this.showAktivnostModal = true;
  }

  openEditAktivnostModal(aktivnost: Aktivnost): void {
    this.currentAktivnost = { ...aktivnost };
    this.showAktivnostModal = true;
  }

  saveAktivnost(aktivnost: Aktivnost): void {
    aktivnost.projekatId = this.projekat.id;
    
    if (aktivnost.id) {
      // Update existing
      this.aktivnostService.update(aktivnost.id, aktivnost).subscribe({
        next: () => {
          this.loadAktivnosti(this.projekat.id);
          this.closeAktivnostModal();
        },
        error: (error) => {
          console.error('Error updating aktivnost:', error);
        }
      });
    } else {
      // Create new
      this.aktivnostService.create(aktivnost).subscribe({
        next: () => {
          this.loadAktivnosti(this.projekat.id);
          this.closeAktivnostModal();
        },
        error: (error) => {
          console.error('Error creating aktivnost:', error);
        }
      });
    }
  }

  closeAktivnostModal(): void {
    this.showAktivnostModal = false;
  }

  onAktivnostDeleted(id: number): void {
    // Activity was deleted from modal, refresh the list
    this.loadAktivnosti(this.projekat.id);
  }

  refreshAktivnosti(): void {
    // Refresh aktivnosti for current project
    if (this.projekat && this.projekat.id) {
      this.loadAktivnosti(this.projekat.id);
    }
  }

  toggleDropdown(aktivnostId: number): void {
    this.openDropdownId = this.openDropdownId === aktivnostId ? null : aktivnostId;
  }

  deleteAktivnost(id: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete ovu aktivnost?')) {
      this.aktivnostService.delete(id).subscribe({
        next: () => {
          this.loadAktivnosti(this.projekat.id);
        },
        error: (error) => {
          console.error('Error deleting aktivnost:', error);
          alert('Greška pri brisanju aktivnosti');
        }
      });
    }
  }

  // Dokument methods
  loadDokumenti(projekatId: number): void {
    this.dokumentService.getByProjekatId(projekatId).subscribe({
      next: (data) => {
        this.dokumenti = data;
      },
      error: (error) => {
        console.error('Error loading dokumenti:', error);
      }
    });
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file && !this.isNewMode) {
      this.uploadFile(file);
    }
  }

  uploadFile(file: File): void {
    this.uploadingFile = true;
    this.dokumentService.uploadDokument(this.projekat.id, file).subscribe({
      next: () => {
        this.loadDokumenti(this.projekat.id);
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
          this.loadDokumenti(this.projekat.id);
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
      'txt': '📝'
    };
    return icons[tipFajla.toLowerCase()] || '📎';
  }

  toggleDokumentiSidebar(): void {
    this.showDokumentiSidebar = !this.showDokumentiSidebar;
    if (this.showDokumentiSidebar) {
      this.showNotesSidebar = false;
    }
  }

  toggleNotesSidebar(): void {
    this.showNotesSidebar = !this.showNotesSidebar;
    if (this.showNotesSidebar) {
      this.showDokumentiSidebar = false;
    }
  }

  // Note methods
  loadNotes(projekatId: number): void {
    this.noteService.getByProjekatId(projekatId).subscribe({
      next: (data) => {
        this.notes = data;
      },
      error: (error) => {
        console.error('Error loading notes:', error);
      }
    });
  }

  saveNote(): void {
    if (!this.currentNote.trim()) return;

    if (this.editingNoteId) {
      // Update existing note
      const noteToUpdate = {
        id: this.editingNoteId,
        projekatId: this.projekat.id,
        opis: this.currentNote
      };
      
      this.noteService.update(this.editingNoteId, noteToUpdate as Note).subscribe({
        next: () => {
          this.loadNotes(this.projekat.id);
          this.currentNote = '';
          this.editingNoteId = null;
        },
        error: (error) => {
          console.error('Error updating note:', error);
          alert('Greška pri ažuriranju beleške');
        }
      });
    } else {
      // Create new note
      const newNote = {
        projekatId: this.projekat.id,
        opis: this.currentNote
      };
      
      this.noteService.create(newNote as Note).subscribe({
        next: () => {
          this.loadNotes(this.projekat.id);
          this.currentNote = '';
        },
        error: (error) => {
          console.error('Error creating note:', error);
          alert('Greška pri kreiranju beleške');
        }
      });
    }
  }

  editNote(note: Note): void {
    this.currentNote = note.opis;
    this.editingNoteId = note.id;
  }

  cancelEditNote(): void {
    this.currentNote = '';
    this.editingNoteId = null;
  }

  deleteNote(id: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete ovu belešku?')) {
      this.noteService.delete(id).subscribe({
        next: () => {
          this.loadNotes(this.projekat.id);
          if (this.editingNoteId === id) {
            this.currentNote = '';
            this.editingNoteId = null;
          }
        },
        error: (error) => {
          console.error('Error deleting note:', error);
          alert('Greška pri brisanju beleške');
        }
      });
    }
  }

  truncateText(text: string, maxLength: number = 30): string {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
  }
}
