import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, Observable } from 'rxjs';
import { AktivnostService } from '../../services/aktivnost.service';
import { KlijentService } from '../../services/klijent.service';
import { PoseteGorivoService } from '../../services/posete-gorivo.service';
import { Aktivnost } from '../../models/aktivnost.model';
import { Klijent } from '../../models/klijent.model';
import { ClientVisit, FuelPurchase } from '../../models/posete-gorivo.model';

type ActiveTab = 'visits' | 'fuel';

@Component({
  selector: 'app-posete-gorivo',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './posete-gorivo.component.html',
  styleUrls: ['./posete-gorivo.component.css']
})
export class PoseteGorivoComponent implements OnInit {
  activeTab: ActiveTab = 'visits';
  visits: ClientVisit[] = [];
  fuelPurchases: FuelPurchase[] = [];
  klijenti: Klijent[] = [];
  aktivnosti: Aktivnost[] = [];
  loading = true;

  visitModalOpen = false;
  fuelModalOpen = false;
  selectedVisit: ClientVisit = this.getEmptyVisit();
  selectedFuelPurchase: FuelPurchase = this.getEmptyFuelPurchase();

  constructor(
    private poseteGorivoService: PoseteGorivoService,
    private klijentService: KlijentService,
    private aktivnostService: AktivnostService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    forkJoin({
      visits: this.poseteGorivoService.getVisits(),
      fuelPurchases: this.poseteGorivoService.getFuelPurchases(),
      klijenti: this.klijentService.getAll(),
      aktivnosti: this.aktivnostService.getAll()
    }).subscribe({
      next: ({ visits, fuelPurchases, klijenti, aktivnosti }) => {
        this.visits = visits;
        this.fuelPurchases = fuelPurchases;
        this.klijenti = klijenti;
        this.aktivnosti = aktivnosti;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading posete i gorivo:', error);
        this.loading = false;
      }
    });
  }

  setActiveTab(tab: ActiveTab): void {
    this.activeTab = tab;
  }

  addVisit(): void {
    this.selectedVisit = this.getEmptyVisit();
    this.visitModalOpen = true;
  }

  editVisit(visit: ClientVisit): void {
    this.selectedVisit = { ...visit };
    this.visitModalOpen = true;
  }

  saveVisit(): void {
    if (!this.selectedVisit.klijentId || !this.selectedVisit.aktivnostId) {
      alert('Klijent i aktivnost su obavezni.');
      return;
    }

    const request = {
      ...this.selectedVisit,
      kilometraza: Number(this.selectedVisit.kilometraza || 0),
      gorivoLitara: Number(this.selectedVisit.gorivoLitara || 0)
    };

    const save$: Observable<unknown> = request.id > 0
      ? this.poseteGorivoService.updateVisit(request.id, request)
      : this.poseteGorivoService.createVisit(request);

    save$.subscribe({
      next: () => {
        this.visitModalOpen = false;
        this.loadData();
      },
      error: (error: any) => {
        console.error('Error saving visit:', error);
        alert('Greška prilikom čuvanja posete.');
      }
    });
  }

  deleteVisit(visit: ClientVisit): void {
    if (!confirm('Da li ste sigurni da želite da obrišete ovu posetu?')) {
      return;
    }

    this.poseteGorivoService.deleteVisit(visit.id).subscribe({
      next: () => this.loadData(),
      error: (error) => {
        console.error('Error deleting visit:', error);
        alert('Greška prilikom brisanja posete.');
      }
    });
  }

  closeVisitModal(): void {
    this.visitModalOpen = false;
    this.selectedVisit = this.getEmptyVisit();
  }

  addFuelPurchase(): void {
    this.selectedFuelPurchase = this.getEmptyFuelPurchase();
    this.fuelModalOpen = true;
  }

  editFuelPurchase(fuelPurchase: FuelPurchase): void {
    this.selectedFuelPurchase = {
      ...fuelPurchase,
      datum: this.toDateInputValue(fuelPurchase.datum)
    };
    this.recalculateFuelTotal();
    this.fuelModalOpen = true;
  }

  saveFuelPurchase(): void {
    const request = {
      ...this.selectedFuelPurchase,
      datum: this.toDate(this.selectedFuelPurchase.datum),
      kolicina: Number(this.selectedFuelPurchase.kolicina || 0),
      jedinicnaCena: Number(this.selectedFuelPurchase.jedinicnaCena || 0),
      ukupnaCena: this.calculateFuelTotal()
    };

    const save$: Observable<unknown> = request.id > 0
      ? this.poseteGorivoService.updateFuelPurchase(request.id, request)
      : this.poseteGorivoService.createFuelPurchase(request);

    save$.subscribe({
      next: () => {
        this.fuelModalOpen = false;
        this.loadData();
      },
      error: (error: any) => {
        console.error('Error saving fuel purchase:', error);
        alert('Greška prilikom čuvanja sipanja goriva.');
      }
    });
  }

  deleteFuelPurchase(fuelPurchase: FuelPurchase): void {
    if (!confirm('Da li ste sigurni da želite da obrišete ovo sipanje goriva?')) {
      return;
    }

    this.poseteGorivoService.deleteFuelPurchase(fuelPurchase.id).subscribe({
      next: () => this.loadData(),
      error: (error) => {
        console.error('Error deleting fuel purchase:', error);
        alert('Greška prilikom brisanja sipanja goriva.');
      }
    });
  }

  closeFuelModal(): void {
    this.fuelModalOpen = false;
    this.selectedFuelPurchase = this.getEmptyFuelPurchase();
  }

  recalculateFuelTotal(): void {
    this.selectedFuelPurchase.ukupnaCena = this.calculateFuelTotal();
  }

  formatDate(date: Date | string | undefined): string {
    if (!date) return '-';
    return this.toDate(date).toLocaleDateString('sr-RS', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  formatDecimal(value: number | undefined): string {
    return Number(value || 0).toLocaleString('sr-RS', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
  }

  getActivityLabel(aktivnost: Aktivnost | undefined): string {
    if (!aktivnost) return '-';
    return `${this.formatDate(aktivnost.datum)} - ${aktivnost.opis}`;
  }

  private calculateFuelTotal(): number {
    const quantity = Number(this.selectedFuelPurchase.kolicina || 0);
    const unitPrice = Number(this.selectedFuelPurchase.jedinicnaCena || 0);
    return Math.round(quantity * unitPrice * 100) / 100;
  }

  private getEmptyVisit(): ClientVisit {
    return {
      id: 0,
      klijentId: 0,
      aktivnostId: 0,
      grad: '',
      kilometraza: 0,
      gorivoLitara: 0
    };
  }

  private getEmptyFuelPurchase(): FuelPurchase {
    return {
      id: 0,
      datum: this.toDateInputValue(new Date()),
      kolicina: 0,
      jedinicnaCena: 0,
      ukupnaCena: 0
    };
  }

  private toDateInputValue(date: Date | string): string {
    const parsedDate = this.toDate(date);
    const year = parsedDate.getFullYear();
    const month = String(parsedDate.getMonth() + 1).padStart(2, '0');
    const day = String(parsedDate.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private toDate(date: Date | string): Date {
    return date instanceof Date ? date : new Date(date);
  }
}
