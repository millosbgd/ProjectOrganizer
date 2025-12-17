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

@Component({
  selector: 'app-projekat-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
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
  
  newAktivnost: Aktivnost = {
    id: 0,
    opis: '',
    datum: new Date(),
    status: 'Planirana',
    vrsta: 'Razvoj',
    projekatId: 0
  };
  
  showAktivnostForm = false;

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
      this.projekatService.create(this.projekat).subscribe({
        next: (data) => {
          this.router.navigate(['/projekti']);
        },
        error: (error) => {
          console.error('Error creating projekat:', error);
        }
      });
    } else {
      this.projekatService.update(this.projekat.id, this.projekat).subscribe({
        next: () => {
          this.isEditMode = false;
          this.loadProjekat(this.projekat.id);
        },
        error: (error) => {
          console.error('Error updating projekat:', error);
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
    this.newAktivnost.projekatId = this.projekat.id;
    this.aktivnostService.create(this.newAktivnost).subscribe({
      next: () => {
        this.loadAktivnosti(this.projekat.id);
        this.showAktivnostForm = false;
        this.resetAktivnostForm();
      },
      error: (error) => {
        console.error('Error creating aktivnost:', error);
      }
    });
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

  resetAktivnostForm(): void {
    this.newAktivnost = {
      id: 0,
      opis: '',
      datum: new Date(),
      status: 'Planirana',
      vrsta: 'Razvoj',
      projekatId: this.projekat.id
    };
  }
}
