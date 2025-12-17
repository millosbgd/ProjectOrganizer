import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { KlijentService } from '../../services/klijent.service';
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
  loading = true;
  openDropdownId: number | null = null;

  constructor(private klijentService: KlijentService) { }

  ngOnInit(): void {
    this.loadKlijenti();
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
