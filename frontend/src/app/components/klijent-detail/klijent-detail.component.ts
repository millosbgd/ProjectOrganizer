import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { KlijentService } from '../../services/klijent.service';
import { CodebookService, CodebookEntry } from '../../services/codebook.service';
import { Klijent } from '../../models/klijent.model';

@Component({
  selector: 'app-klijent-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './klijent-detail.component.html',
  styleUrls: ['./klijent-detail.component.css']
})
export class KlijentDetailComponent implements OnInit {
  klijent: Klijent = {
    id: 0,
    naziv: '',
    adresa: '',
    grad: '',
    zemlja: ''
  };
  
  countries: CodebookEntry[] = [];
  isEditMode = false;
  isNewMode = false;
  loading = true;

  constructor(
    private klijentService: KlijentService,
    private codebookService: CodebookService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCountries();
    
    const id = this.route.snapshot.paramMap.get('id');
    
    if (id === 'new') {
      this.isNewMode = true;
      this.isEditMode = true;
      this.loading = false;
    } else if (id) {
      this.loadKlijent(+id);
    }
  }

  loadCountries(): void {
    this.codebookService.getByType('Country').subscribe({
      next: (data) => {
        this.countries = data;
      },
      error: (error) => {
        console.error('Error loading countries:', error);
      }
    });
  }

  loadKlijent(id: number): void {
    this.klijentService.getById(id).subscribe({
      next: (data) => {
        this.klijent = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading klijent:', error);
        this.loading = false;
      }
    });
  }

  saveKlijent(): void {
    if (this.isNewMode) {
      this.klijentService.create(this.klijent).subscribe({
        next: (data) => {
          this.router.navigate(['/klijenti']);
        },
        error: (error) => {
          console.error('Error creating klijent:', error);
        }
      });
    } else {
      this.klijentService.update(this.klijent.id, this.klijent).subscribe({
        next: () => {
          this.router.navigate(['/klijenti']);
        },
        error: (error) => {
          console.error('Error updating klijent:', error);
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/klijenti']);
  }

  getCountryName(code: string): string {
    const country = this.countries.find(c => c.code === code);
    return country ? country.value : code;
  }
}
