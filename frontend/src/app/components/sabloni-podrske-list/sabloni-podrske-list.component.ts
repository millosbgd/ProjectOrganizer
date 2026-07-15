import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SablonPodrske } from '../../models/sablon-podrske.model';
import { SablonPodrskeService } from '../../services/sablon-podrske.service';
import { SablonPodrskeModalComponent } from '../sablon-podrske-modal/sablon-podrske-modal.component';

@Component({
  selector: 'app-sabloni-podrske-list',
  standalone: true,
  imports: [CommonModule, SablonPodrskeModalComponent],
  templateUrl: './sabloni-podrske-list.component.html',
  styleUrls: ['./sabloni-podrske-list.component.css']
})
export class SabloniPodrskeListComponent implements OnInit {
  sabloni: SablonPodrske[] = [];
  loading = true;
  showAllTemplates = false;
  isModalOpen = false;
  selectedSablon: SablonPodrske = this.getEmptySablon();

  constructor(private sablonPodrskeService: SablonPodrskeService) {}

  ngOnInit(): void {
    this.loadSabloni();
  }

  loadSabloni(): void {
    this.loading = true;
    this.sablonPodrskeService.getAll(!this.showAllTemplates).subscribe({
      next: (data) => {
        this.sabloni = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading support templates:', error);
        this.loading = false;
      }
    });
  }

  toggleShowAll(): void {
    this.showAllTemplates = !this.showAllTemplates;
    this.loadSabloni();
  }

  addNewSablon(): void {
    this.selectedSablon = this.getEmptySablon();
    this.isModalOpen = true;
  }

  editSablon(sablon: SablonPodrske): void {
    this.selectedSablon = { ...sablon };
    this.isModalOpen = true;
  }

  saveSablon(sablon: SablonPodrske): void {
    if (sablon.id && sablon.id > 0) {
      this.sablonPodrskeService.update(sablon.id, sablon).subscribe({
        next: () => {
          this.closeModal();
          this.loadSabloni();
        },
        error: (error) => {
          console.error('Error updating support template:', error);
          alert('Greška prilikom ažuriranja šablona za podršku.');
        }
      });
      return;
    }

    this.sablonPodrskeService.create(sablon).subscribe({
      next: () => {
        this.closeModal();
        this.loadSabloni();
      },
      error: (error) => {
        console.error('Error creating support template:', error);
        alert('Greška prilikom kreiranja šablona za podršku.');
      }
    });
  }

  deleteSablon(id: number): void {
    this.sablonPodrskeService.delete(id).subscribe({
      next: () => {
        this.closeModal();
        this.loadSabloni();
      },
      error: (error) => {
        console.error('Error deleting support template:', error);
        alert('Greška prilikom brisanja šablona za podršku.');
      }
    });
  }

  deleteSablonFromList(id: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete šablon za podršku?')) {
      this.deleteSablon(id);
    }
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.selectedSablon = this.getEmptySablon();
  }

  formatDate(date?: string): string {
    if (!date) {
      return '-';
    }

    return new Date(date).toLocaleDateString('sr-RS', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
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
