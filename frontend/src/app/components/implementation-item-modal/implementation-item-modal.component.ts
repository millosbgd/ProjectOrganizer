import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProjectImplementationItem, ProjectImplementationCheckListItem } from '../../models/project-implementation-item.model';
import { ProjectImplementationItemService } from '../../services/project-implementation-item.service';

@Component({
  selector: 'app-implementation-item-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './implementation-item-modal.component.html',
  styleUrl: './implementation-item-modal.component.css'
})
export class ImplementationItemModalComponent {
  @Input() item: ProjectImplementationItem | null = null;
  @Input() isOpen = false;
  @Output() save = new EventEmitter<ProjectImplementationItem>();
  @Output() close = new EventEmitter<void>();

  constructor(private implementationItemService: ProjectImplementationItemService) {}

  get zavrsenoDateRequired(): boolean {
    return this.item?.zavrseno === true;
  }

  get klijentPotvrdioDateRequired(): boolean {
    return this.item?.klijentPotvrdio === true;
  }

  get allCheckListsCompleted(): boolean {
    if (!this.item?.checkLists || this.item.checkLists.length === 0) {
      return true; // No checklists means no restriction
    }
    return this.item.checkLists.every(cl => cl.zavrsen);
  }

  onCheckListToggle(checkListItem: ProjectImplementationCheckListItem): void {
    if (!this.item) return;

    const newStatus = !checkListItem.zavrsen;
    this.implementationItemService.updateCheckList(this.item.id, checkListItem.id, newStatus).subscribe({
      next: () => {
        checkListItem.zavrsen = newStatus;
        checkListItem.zavrsenDatum = newStatus ? new Date() : undefined;
      },
      error: (error) => {
        console.error('Error updating checklist item:', error);
        alert('Greška pri ažuriranju stavke čekliste');
      }
    });
  }

  onCheckListKlijentPotvrdioToggle(checkListItem: ProjectImplementationCheckListItem): void {
    if (!this.item) return;

    const newStatus = !checkListItem.klijentPotvrdio;
    this.implementationItemService.updateCheckListKlijentPotvrdio(this.item.id, checkListItem.id, newStatus).subscribe({
      next: () => {
        checkListItem.klijentPotvrdio = newStatus;
        checkListItem.klijentPotvrdioDatum = newStatus ? new Date() : undefined;
      },
      error: (error) => {
        console.error('Error updating checklist klijent potvrda:', error);
        alert('Greška pri ažuriranju potvrde klijenta');
      }
    });
  }

  onZavrsenoChange(): void {
    if (this.item) {
      if (!this.item.zavrseno) {
        this.item.zavrsenoDatum = undefined;
      } else if (!this.allCheckListsCompleted) {
        this.item.zavrseno = false;
        alert('Sve stavke čekliste moraju biti završene pre nego što označite stavku kao završenu.');
      }
    }
  }

  onKlijentPotvrdioChange(): void {
    if (this.item) {
      if (!this.item.klijentPotvrdio) {
        this.item.klijentPotvrdioDatum = undefined;
      } else if (!this.allCheckListsCompleted) {
        this.item.klijentPotvrdio = false;
        alert('Sve stavke čekliste moraju biti završene pre nego da klijent potvrdi.');
      }
    }
  }

  onSave(): void {
    if (this.item) {
      // Validate required fields
      if (this.item.zavrseno && !this.item.zavrsenoDatum) {
        alert('Datum završetka je obavezan kada je stavka označena kao završena.');
        return;
      }
      if (this.item.klijentPotvrdio && !this.item.klijentPotvrdioDatum) {
        alert('Datum potvrde klijenta je obavezan kada je stavka potvrđena od strane klijenta.');
        return;
      }
      this.save.emit(this.item);
    }
  }

  onClose(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  get zavrsenoDatumString(): string {
    if (!this.item?.zavrsenoDatum) return '';
    const date = new Date(this.item.zavrsenoDatum);
    return date.toISOString().split('T')[0];
  }

  set zavrsenoDatumString(value: string) {
    if (this.item) {
      this.item.zavrsenoDatum = value ? new Date(value) : undefined;
    }
  }

  get klijentPotvrdioDatumString(): string {
    if (!this.item?.klijentPotvrdioDatum) return '';
    const date = new Date(this.item.klijentPotvrdioDatum);
    return date.toISOString().split('T')[0];
  }

  set klijentPotvrdioDatumString(value: string) {
    if (this.item) {
      this.item.klijentPotvrdioDatum = value ? new Date(value) : undefined;
    }
  }
}
