import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProjekatService } from '../../services/projekat.service';
import { KlijentService } from '../../services/klijent.service';
import { AktivnostService } from '../../services/aktivnost.service';
import { Projekat } from '../../models/projekat.model';
import { Klijent } from '../../models/klijent.model';
import { Aktivnost } from '../../models/aktivnost.model';
import { AktivnostModalComponent } from '../aktivnost-modal/aktivnost-modal.component';

@Component({
  selector: 'app-projekat-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AktivnostModalComponent],
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
    status: 'U pripremi',
    klijentId: 0
  };
  
  klijenti: Klijent[] = [];
  aktivnosti: Aktivnost[] = [];
  isEditMode = false;
  isNewMode = false;
  loading = true;
  
  currentAktivnost: Aktivnost = {
    id: 0,
    opis: '',
    datum: new Date(),
    status: 'Planirana',
    vrsta: 'Razvoj',
    projekatId: 0
  };
  
  showAktivnostModal = false;
  openDropdownId: number | null = null;

  constructor(
    private projekatService: ProjekatService,
    private klijentService: KlijentService,
    private aktivnostService: AktivnostService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadKlijenti();
    
    const id = this.route.snapshot.paramMap.get('id');
    
    if (id === 'new') {
      this.isNewMode = true;
      this.isEditMode = true;
      this.loading = false;
    } else if (id) {
      this.loadProjekat(+id);
      this.loadAktivnosti(+id);
    }
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

  loadProjekat(id: number): void {
    this.projekatService.getById(id).subscribe({
      next: (data) => {
        this.projekat = data;
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

  saveProjekat(): void {
    if (this.isNewMode) {
      // For new projects, send empty brojProjekta (backend will generate it)
      const projekatToSave = {
        id: 0,
        brojProjekta: '',
        naziv: this.projekat.naziv,
        datum: this.projekat.datum,
        aktivan: this.projekat.aktivan,
        status: this.projekat.status,
        klijentId: this.projekat.klijentId
      };

      this.projekatService.create(projekatToSave).subscribe({
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
        datum: this.projekat.datum,
        naziv: this.projekat.naziv,
        aktivan: this.projekat.aktivan,
        status: this.projekat.status,
        klijentId: this.projekat.klijentId
      };

      this.projekatService.update(this.projekat.id, projekatToSave).subscribe({
        next: () => {
          this.isEditMode = false;
          this.loadProjekat(this.projekat.id);
        },
        error: (error) => {
          console.error('Error updating projekat:', error);
          console.error('Error details:', error.error);
          alert('Greška pri ažuriranju projekta: ' + (error.error || error.message));
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
        }
      });
    }
  }
}
