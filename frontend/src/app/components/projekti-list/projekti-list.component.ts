import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProjekatService } from '../../services/projekat.service';
import { UserService } from '../../services/user.service';
import { Projekat } from '../../models/projekat.model';
import { User } from '../../models/user.model';

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
  openDropdownId: number | null = null;
  showAllProjects = false;
  currentUser: User | null = null;

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
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading projekti:', error);
        this.loading = false;
      }
    });
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
