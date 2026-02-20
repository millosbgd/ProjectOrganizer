import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AktivnostService } from '../../services/aktivnost.service';
import { UserService } from '../../services/user.service';
import { Aktivnost } from '../../models/aktivnost.model';
import { User } from '../../models/user.model';
import { AktivnostModalComponent } from '../aktivnost-modal/aktivnost-modal.component';

@Component({
  selector: 'app-aktivnosti-list',
  standalone: true,
  imports: [CommonModule, RouterModule, AktivnostModalComponent],
  templateUrl: './aktivnosti-list.component.html',
  styleUrls: ['./aktivnosti-list.component.css']
})
export class AktivnostiListComponent implements OnInit {
  aktivnosti: Aktivnost[] = [];
  loading = true;
  showAllActivities = false;
  currentUser: User | null = null;
  
  // Modal properties
  isModalOpen = false;
  selectedAktivnost: Aktivnost = this.getEmptyAktivnost();

  constructor(
    private aktivnostService: AktivnostService,
    private userService: UserService
  ) { }

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadAktivnosti();
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

  loadAktivnosti(): void {
    this.loading = true;
    const myActivitiesOnly = !this.showAllActivities;
    
    this.aktivnostService.getAll(myActivitiesOnly).subscribe({
      next: (data) => {
        this.aktivnosti = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading aktivnosti:', error);
        this.loading = false;
      }
    });
  }

  toggleShowAll(): void {
    this.showAllActivities = !this.showAllActivities;
    this.loadAktivnosti();
  }

  deleteAktivnost(id: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete ovu aktivnost?')) {
      this.aktivnostService.delete(id).subscribe({
        next: () => {
          this.loadAktivnosti();
        },
        error: (error) => {
          console.error('Error deleting aktivnost:', error);
        }
      });
    }
  }

  editAktivnost(aktivnost: Aktivnost): void {
    this.selectedAktivnost = { ...aktivnost };
    this.isModalOpen = true;
  }

  addNewAktivnost(): void {
    this.selectedAktivnost = this.getEmptyAktivnost();
    this.isModalOpen = true;
  }

  onModalSave(aktivnost: Aktivnost): void {
    if (aktivnost.id && aktivnost.id > 0) {
      // Update existing
      this.aktivnostService.update(aktivnost.id, aktivnost).subscribe({
        next: () => {
          this.isModalOpen = false;
          this.loadAktivnosti();
        },
        error: (error) => {
          console.error('Error updating aktivnost:', error);
          alert('Greška prilikom ažuriranja aktivnosti.');
        }
      });
    } else {
      // Create new
      this.aktivnostService.create(aktivnost).subscribe({
        next: () => {
          this.isModalOpen = false;
          this.loadAktivnosti();
        },
        error: (error) => {
          console.error('Error creating aktivnost:', error);
          alert('Greška prilikom kreiranja aktivnosti.');
        }
      });
    }
  }

  onModalClose(): void {
    this.isModalOpen = false;
    this.selectedAktivnost = this.getEmptyAktivnost();
  }

  onModalDelete(id: number): void {
    this.isModalOpen = false;
    this.loadAktivnosti();
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('sr-RS', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  private getEmptyAktivnost(): Aktivnost {
    return {
      id: 0,
      opis: '',
      detalji: '',
      datum: new Date(),
      status: 'Planirana',
      vrsta: 'Razvoj',
      bau: false,
      projekatId: null
    };
  }
}
