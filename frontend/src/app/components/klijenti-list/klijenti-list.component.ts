import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { KlijentService } from '../../services/klijent.service';
import { CodebookService, CodebookEntry } from '../../services/codebook.service';
import { Klijent } from '../../models/klijent.model';

@Component({
  selector: 'app-klijenti-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './klijenti-list.component.html',
  styleUrls: ['./klijenti-list.component.css']
})
export class KlijentiListComponent implements OnInit {
  klijenti: Klijent[] = [];
  countries: CodebookEntry[] = [];
  loading = true;
  openDropdownId: number | null = null;

  constructor(
    private klijentService: KlijentService,
    private codebookService: CodebookService
  ) { }

  ngOnInit(): void {
    this.loadCountries();
    this.loadKlijenti();
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

  getCountryName(code: string): string {
    const country = this.countries.find(c => c.code === code);
    return country ? country.value : code;
  }

  loadKlijenti(): void {
    this.klijentService.getAll().subscribe({
      next: (data) => {
        this.klijenti = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading klijenti:', error);
        this.loading = false;
      }
    });
  }

  deleteKlijent(id: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete ovog klijenta?')) {
      this.klijentService.delete(id).subscribe({
        next: () => {
          this.loadKlijenti();
        },
        error: (error) => {
          console.error('Error deleting klijent:', error);
        }
      });
    }
  }

  toggleDropdown(event: Event, klijentId: number): void {
    event.stopPropagation();
    this.openDropdownId = this.openDropdownId === klijentId ? null : klijentId;
  }
}
