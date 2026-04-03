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

  newCustomOpis = '';
  newCustomDetaljanOpis = '';
  newCustomPlaniraniRok = '';
  showCheckListItemModal = false;
  checkListItemModalMode: 'add' | 'edit' = 'add';
  editingCheckListItem: ProjectImplementationCheckListItem | null = null;
  clModalOpis = '';
  clModalDetaljanOpis = '';
  clModalPlaniraniRok = '';
  clModalKompleksnost: number | null = null;

  constructor(private implementationItemService: ProjectImplementationItemService) {}

  isCustomItem(cl: ProjectImplementationCheckListItem): boolean {
    return cl.checkListItemId === -1;
  }
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
        checkListItem.zavrsenDatum = newStatus ? new Date().toISOString().split('T')[0] : undefined;
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
        checkListItem.klijentPotvrdioDatum = newStatus ? new Date().toISOString().split('T')[0] : undefined;
      },
      error: (error) => {
        console.error('Error updating checklist klijent potvrda:', error);
        alert('Greška pri ažuriranju potvrde klijenta');
      }
    });
  }
  startEditCheckList(cl: ProjectImplementationCheckListItem): void {
    this.editingCheckListItem = cl;
    this.clModalOpis = cl.checkListItemOpis || '';
    this.clModalDetaljanOpis = cl.detaljanOpis || '';
    this.clModalPlaniraniRok = cl.planiraniRok || '';
    this.clModalKompleksnost = cl.kompleksnost ?? null;
    this.checkListItemModalMode = 'edit';
    this.showCheckListItemModal = true;
  }

  saveCheckListFields(cl: ProjectImplementationCheckListItem): void {
    if (!this.item) return;
    cl.detaljanOpis = this.clModalDetaljanOpis || undefined;
    cl.planiraniRok = this.clModalPlaniraniRok || undefined;
    if (this.isCustomItem(cl)) {
      cl.checkListItemOpis = this.clModalOpis;
      cl.kompleksnost = this.clModalKompleksnost;
    }
    this.implementationItemService.updateCheckListFields(this.item.id, cl.id, {
      detaljanOpis: cl.detaljanOpis ?? null,
      planiraniRok: cl.planiraniRok ?? null,
      clearPlaniraniRok: !cl.planiraniRok,
      ...(this.isCustomItem(cl) ? { kompleksnost: this.clModalKompleksnost, opis: this.clModalOpis || null } : {})
    }).subscribe({
      next: (updatedItems) => {
        // If backend returns recalculated items, update all procenat values
        if (Array.isArray(updatedItems) && this.item?.checkLists) {
          for (const updated of updatedItems) {
            const local = this.item.checkLists.find(c => c.id === updated.id);
            if (local) local.procenat = updated.procenat;
          }
        }
        this.showCheckListItemModal = false;
        this.editingCheckListItem = null;
      },
      error: (error) => {
        console.error('Error saving checklist fields:', error);
        alert('Greška pri čuvanju polja stavke čekliste');
      }
    });
  }

  cancelCheckListItemModal(): void {
    this.showCheckListItemModal = false;
    this.editingCheckListItem = null;
    this.clModalOpis = '';
    this.clModalDetaljanOpis = '';
    this.clModalPlaniraniRok = '';
    this.clModalKompleksnost = null;
  }

  openAddCustomModal(): void {
    this.checkListItemModalMode = 'add';
    this.clModalOpis = '';
    this.clModalDetaljanOpis = '';
    this.clModalPlaniraniRok = '';
    this.clModalKompleksnost = null;
    this.showCheckListItemModal = true;
  }

  addCustomCheckListItem(): void {
    if (!this.item || !this.clModalOpis.trim()) {
      alert('Opis je obavezan.');
      return;
    }
    this.implementationItemService.addCustomCheckListItem(this.item.id, {
      opis: this.clModalOpis.trim(),
      detaljanOpis: this.clModalDetaljanOpis || null,
      planiraniRok: this.clModalPlaniraniRok || null,
      kompleksnost: this.clModalKompleksnost
    }).subscribe({
      next: (newCl) => {
        // Backend recalculates all procenat, so refresh from server
        this.item!.checkLists = [...(this.item!.checkLists || []), {
          ...newCl,
          checkListItemOpis: newCl.checkListItemOpis,
          zavrsen: newCl.zavrsen ?? false,
          klijentPotvrdio: newCl.klijentPotvrdio ?? false
        }];
        // Reload the item to get updated procenat for all items
        this.implementationItemService.getById(this.item!.id).subscribe({
          next: (updated) => { this.item!.checkLists = updated.checkLists; }
        });
        this.showCheckListItemModal = false;
        this.clModalOpis = '';
        this.clModalDetaljanOpis = '';
        this.clModalPlaniraniRok = '';
        this.clModalKompleksnost = null;
      },
      error: (error) => {
        console.error('Error adding custom checklist item:', error);
        alert('Greška pri dodavanju stavke čekliste');
      }
    });
  }

  deleteCustomCheckListItem(cl: ProjectImplementationCheckListItem): void {
    if (!this.item) return;
    if (!confirm(`Obriši stavku "${cl.checkListItemOpis}"?`)) return;
    this.implementationItemService.deleteCustomCheckListItem(this.item.id, cl.id).subscribe({
      next: () => {
        this.item!.checkLists = this.item!.checkLists!.filter(c => c.id !== cl.id);
        // Reload to get updated procenat after recalculation
        this.implementationItemService.getById(this.item!.id).subscribe({
          next: (updated) => { this.item!.checkLists = updated.checkLists; }
        });
      },
      error: (error) => {
        console.error('Error deleting custom checklist item:', error);
        alert('Greška pri brisanju stavke čekliste');
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
    return this.item?.zavrsenoDatum ?? '';
  }

  set zavrsenoDatumString(value: string) {
    if (this.item) {
      this.item.zavrsenoDatum = value || undefined;
    }
  }

  get klijentPotvrdioDatumString(): string {
    return this.item?.klijentPotvrdioDatum ?? '';
  }

  set klijentPotvrdioDatumString(value: string) {
    if (this.item) {
      this.item.klijentPotvrdioDatum = value || undefined;
    }
  }

  get totalProcenat(): number {
    return (this.item?.checkLists ?? []).reduce((s, cl) => s + (cl.procenat ?? 0), 0);
  }

  get zavrsenoProcenat(): number {
    return (this.item?.checkLists ?? []).filter(cl => cl.zavrsen).reduce((s, cl) => s + (cl.procenat ?? 0), 0);
  }

  get klijentPotvrdioProcenat(): number {
    return (this.item?.checkLists ?? []).filter(cl => cl.klijentPotvrdio).reduce((s, cl) => s + (cl.procenat ?? 0), 0);
  }
}
