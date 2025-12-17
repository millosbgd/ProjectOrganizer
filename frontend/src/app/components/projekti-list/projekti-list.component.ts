import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProjekatService } from '../../services/projekat.service';
import { Projekat } from '../../models/projekat.model';

@Component({
  selector: 'app-projekti-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './projekti-list.component.html',
  styleUrls: ['./projekti-list.component.css']
})
export class ProjektiListComponent implements OnInit {
  projekti: Projekat[] = [];
  loading = true;

  constructor(private projekatService: ProjekatService) { }

  ngOnInit(): void {
    this.loadProjekti();
  }

  loadProjekti(): void {
    this.projekatService.getAll().subscribe({
      next: (data) => {
        this.projekti = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading projekti:', error);
        this.loading = false;
      }
    });
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
}
