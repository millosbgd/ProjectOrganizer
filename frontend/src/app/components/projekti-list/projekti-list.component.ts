import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProjekatService } from '../../services/projekat.service';
import { UserService } from '../../services/user.service';
import { Projekat } from '../../models/projekat.model';
import { User } from '../../models/user.model';

@Component({
  selector: 'app-projekti-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './projekti-list.component.html',
  styleUrls: ['./projekti-list.component.css']
})
export class ProjektiListComponent implements OnInit {
  projekti: Projekat[] = [];
  filteredProjekti: Projekat[] = [];
  loading = true;
  openDropdownId: number | null = null;
  showAllProjects = false;
  currentUser: User | null = null;

  // Filters
  filterNaziv: string = '';
  filterKlijentId: number | null = null;
  uniqueKlijenti: { id: number, naziv: string }[] = [];

  constructor(
    private projekatService: ProjekatService,
    private userService: UserService
  ) { }

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadProjekti();
  }

  loadCurrentUser(): void {
    this.userService.getCurrentUser().subscribe({
      next: (user) => {
        this.currentUser = user;
      },
      error: (error) => {
        console.error('Error loading current user:', error);
      }
    });
  }

  loadProjekti(): void {
    this.projekatService.getAll(!this.showAllProjects).subscribe({
      next: (data) => {
        this.projekti = data;
        this.extractUniqueKlijenti();
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading projekti:', error);
        this.loading = false;
      }
    });
  }

  extractUniqueKlijenti(): void {
    const klijentiMap = new Map<number, string>();
    this.projekti.forEach(p => {
      if (p.klijent && p.klijent.id) {
        klijentiMap.set(p.klijent.id, p.klijent.naziv);
      }
    });
    this.uniqueKlijenti = Array.from(klijentiMap.entries())
      .map(([id, naziv]) => ({ id, naziv }))
      .sort((a, b) => a.naziv.localeCompare(b.naziv));
  }

  applyFilters(): void {
    let filtered = [...this.projekti];

    console.log('Applying filters - nazivFilter:', this.filterNaziv, 'klijentFilter:', this.filterKlijentId);
    console.log('Total projekti:', this.projekti.length);

    // Filter by naziv
    if (this.filterNaziv && this.filterNaziv.trim()) {
      const searchTerm = this.filterNaziv.toLowerCase().trim();
      filtered = filtered.filter(p => {
        const naziv = p.naziv?.toLowerCase() || '';
        const brojProjekta = p.brojProjekta?.toLowerCase() || '';
        const matches = naziv.includes(searchTerm) || brojProjekta.includes(searchTerm);
        return matches;
      });
      console.log('After naziv filter:', filtered.length);
    }

    // Filter by klijent
    if (this.filterKlijentId) {
      filtered = filtered.filter(p => {
        const matches = p.klijentId === this.filterKlijentId;
        if (!matches) {
          console.log('Projekat', p.naziv, 'klijentId:', p.klijentId, 'vs filter:', this.filterKlijentId);
        }
        return matches;
      });
      console.log('After klijent filter:', filtered.length);
    }

    this.filteredProjekti = filtered;
    console.log('Final filtered count:', this.filteredProjekti.length);
  }

  clearFilters(): void {
    this.filterNaziv = '';
    this.filterKlijentId = null;
    this.applyFilters();
  }

  toggleShowAll(): void {
    this.showAllProjects = !this.showAllProjects;
    this.loading = true;
    this.loadProjekti();
  }

  deleteProjekt(id: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete ovaj projekat?')) {
      this.projekatService.delete(id).subscribe({
        next: () => {
          this.loadProjekti();
        },
        error: (error) => {
          console.error('Error deleting projekat:', error);
        }
      });
    }
  }

  toggleDropdown(event: Event, projektId: number): void {
    event.stopPropagation();
    this.openDropdownId = this.openDropdownId === projektId ? null : projektId;
  }
}
