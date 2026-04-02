import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ImplementationModelService } from '../../services/implementation-model.service';
import { ImplementationModel, ImplementationItem } from '../../models/implementation-model.model';
import { ImplementationItemService } from '../../services/implementation-item.service';
import { CheckListItemService } from '../../services/checklist-item.service';
import { CheckListItem } from '../../models/checklist-item.model';

@Component({
  selector: 'app-implementation-model-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './implementation-model-edit.component.html',
  styleUrls: ['./implementation-model-edit.component.css']
})
export class ImplementationModelEditComponent implements OnInit {
  model: ImplementationModel = {
    id: 0,
    naziv: '',
    opis: '',
    aktivan: true,
    items: []
  };
  isNew = true;
  loading = false;
  saving = false;
  showItemModal = false;
  editingItem: ImplementationItem = { id: 0, implementationModelId: 0, naziv: '', detalji: '', checkListItems: [] };
  editingItemIndex: number | null = null;
  
  // Check list management
  loadingCheckLists = false;
  showCheckListSelectionModal = false;
  availableCheckListItems: CheckListItem[] = [];
  selectedCheckListIds: number[] = [];
  
  // New check list item creation
  showNewCheckListItemModal = false;
  newCheckListItem: { opis: string; kompleksnost: number | null } = { opis: '', kompleksnost: null };
  savingNewCheckListItem = false;

  // Toast & inline error messages
  showToast = false;
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';
  formError = '';
  itemModalError = '';
  checkListSelectionError = '';
  newCheckListItemError = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private implementationModelService: ImplementationModelService,
    private implementationItemService: ImplementationItemService,
    private checkListItemService: CheckListItemService
  ) { }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isNew = false;
      this.loading = true;
      this.loadModel(+id);
    }
  }

  showToastMessage(message: string, type: 'success' | 'error' = 'success'): void {
    this.toastMessage = message;
    this.toastType = type;
    this.showToast = true;
    setTimeout(() => {
      this.showToast = false;
      this.toastMessage = '';
    }, 3000);
  }

  loadModel(id: number): void {
    this.implementationModelService.getById(id).subscribe({
      next: (data) => {
        this.model = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading model:', error);
        this.showToastMessage('Greška pri učitavanju modela', 'error');
        setTimeout(() => this.router.navigate(['/implementation-models']), 2000);
      }
    });
  }

  addItem(): void {
    this.itemModalError = '';
    this.editingItem = {
      id: 0,
      implementationModelId: this.model.id,
      naziv: '',
      detalji: '',
      checkListItems: []
    };
    this.editingItemIndex = null;
    this.showItemModal = true;
  }

  editItem(index: number): void {
    this.editingItemIndex = index;
    this.itemModalError = '';
    // Create a copy to avoid direct editing
    this.editingItem = { ...this.model.items[index], checkListItems: [] };
    this.showItemModal = true;
    
    // Load check list items if editing existing item
    if (this.editingItem.id && this.editingItem.id > 0) {
      this.loadCheckListsForItem(this.editingItem.id);
    }
  }

  loadCheckListsForItem(itemId: number): void {
    this.loadingCheckLists = true;
    this.implementationItemService.getCheckLists(itemId).subscribe({
      next: (checkLists) => {
        this.editingItem.checkListItems = checkLists;
        this.loadingCheckLists = false;
      },
      error: (error) => {
        console.error('Error loading check lists:', error);
        this.itemModalError = 'Greška pri učitavanju čekliste';
        this.loadingCheckLists = false;
      }
    });
  }

  openCheckListSelection(): void {
    // Load all available check list items
    this.loadAvailableCheckListItems();
    this.selectedCheckListIds = [];
    this.checkListSelectionError = '';
    this.showCheckListSelectionModal = true;
  }

  toggleCheckListSelection(id: number): void {
    const index = this.selectedCheckListIds.indexOf(id);
    if (index > -1) {
      this.selectedCheckListIds.splice(index, 1);
    } else {
      this.selectedCheckListIds.push(id);
    }
  }

  isCheckListSelected(id: number): boolean {
    return this.selectedCheckListIds.includes(id);
  }

  saveCheckListSelection(): void {
    if (this.selectedCheckListIds.length === 0) {
      this.checkListSelectionError = 'Molimo izaberite bar jednu stavku';
      return;
    }

    if (!this.editingItem.id || this.editingItem.id === 0) {
      this.checkListSelectionError = 'Molimo prvo sačuvajte stavku modela pre dodavanja čekliste';
      this.showCheckListSelectionModal = false;
      return;
    }

    this.checkListSelectionError = '';
    this.implementationItemService.addCheckLists(this.editingItem.id, {
      checkListItemIds: this.selectedCheckListIds
    }).subscribe({
      next: () => {
        this.showCheckListSelectionModal = false;
        this.selectedCheckListIds = [];
        // Reload check lists
        this.loadCheckListsForItem(this.editingItem.id!);
      },
      error: (error) => {
        console.error('Error adding check lists:', error);
        this.checkListSelectionError = 'Greška pri dodavanju stavki čekliste';
      }
    });
  }

  cancelCheckListSelection(): void {
    this.showCheckListSelectionModal = false;
    this.selectedCheckListIds = [];
  }

  openNewCheckListItemModal(): void {
    this.newCheckListItem = { opis: '', kompleksnost: null };
    this.newCheckListItemError = '';
    this.showNewCheckListItemModal = true;
  }

  closeNewCheckListItemModal(): void {
    this.showNewCheckListItemModal = false;
    this.newCheckListItem = { opis: '', kompleksnost: null };
  }

  saveNewCheckListItem(): void {
    if (!this.newCheckListItem.opis || this.newCheckListItem.opis.trim() === '') {
      this.newCheckListItemError = 'Opis je obavezan';
      return;
    }

    if (this.newCheckListItem.kompleksnost !== null && 
        (this.newCheckListItem.kompleksnost < 1 || this.newCheckListItem.kompleksnost > 10)) {
      this.newCheckListItemError = 'Kompleksnost mora biti između 1 i 10';
      return;
    }

    this.newCheckListItemError = '';
    this.savingNewCheckListItem = true;

    const newItem: CheckListItem = {
      id: 0,
      opis: this.newCheckListItem.opis,
      kompleksnost: this.newCheckListItem.kompleksnost
    };

    this.checkListItemService.create(newItem).subscribe({
      next: (createdItem) => {
        this.savingNewCheckListItem = false;
        this.closeNewCheckListItemModal();
        // Refresh the list
        this.loadAvailableCheckListItems();
        this.showToastMessage('Stavka uspešno kreirana!');
      },
      error: (error) => {
        console.error('Error creating check list item:', error);
        this.newCheckListItemError = 'Greška pri kreiranju stavke';
        this.savingNewCheckListItem = false;
      }
    });
  }

  loadAvailableCheckListItems(): void {
    this.checkListItemService.getAll().subscribe({
      next: (items) => {
        this.availableCheckListItems = items;
      },
      error: (error) => {
        console.error('Error loading check list items:', error);
      }
    });
  }

  removeCheckListItem(linkId: number | undefined): void {
    if (!linkId || !this.editingItem.id) {
      return;
    }

    if (!confirm('Da li ste sigurni da želite da uklonite ovu stavku čekliste?')) {
      return;
    }

    this.implementationItemService.removeCheckList(this.editingItem.id, linkId).subscribe({
      next: () => {
        // Remove from local array
        this.editingItem.checkListItems = this.editingItem.checkListItems?.filter(c => c.linkId !== linkId) || [];
      },
      error: (error) => {
        console.error('Error removing check list item:', error);
        this.itemModalError = 'Greška pri uklanjanju stavke čekliste';
      }
    });
  }

  saveItemFromModal(): void {
    if (!this.editingItem.naziv || this.editingItem.naziv.trim() === '') {
      this.itemModalError = 'Naziv je obavezan';
      return;
    }
    this.itemModalError = '';

    if (this.editingItemIndex !== null) {
      // Update existing item
      this.model.items[this.editingItemIndex] = { ...this.editingItem };
    } else {
      // Add new item
      this.model.items.push({ ...this.editingItem });
    }

    this.closeItemModal();
  }

  closeItemModal(): void {
    this.showItemModal = false;
    this.editingItem = { id: 0, implementationModelId: 0, naziv: '', detalji: '', checkListItems: [] };
    this.editingItemIndex = null;
  }

  removeItem(index: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete ovu stavku?')) {
      this.model.items.splice(index, 1);
    }
  }

  save(): void {
    this.formError = '';
    if (!this.model.naziv || this.model.naziv.trim() === '') {
      this.formError = 'Naziv je obavezan';
      return;
    }

    // Validate items
    for (const item of this.model.items) {
      if (!item.naziv || item.naziv.trim() === '') {
        this.formError = 'Sve stavke moraju imati naziv';
        return;
      }
    }

    this.saving = true;

    if (this.isNew) {
      this.implementationModelService.create(this.model).subscribe({
        next: (createdModel) => {
          this.showToastMessage('Model uspešno kreiran!');
          // Update model with created data (including ID)
          this.model = createdModel;
          this.isNew = false;
          this.saving = false;
        },
        error: (error: any) => {
          console.error('Error saving model:', error);
          this.formError = 'Greška pri čuvanju modela';
          this.saving = false;
        }
      });
    } else {
      this.implementationModelService.update(this.model.id, this.model).subscribe({
        next: () => {
          this.showToastMessage('Model uspešno ažuriran!');
          this.saving = false;
          // Reload model to get fresh data
          this.loadModel(this.model.id);
        },
        error: (error: any) => {
          console.error('Error saving model:', error);
          this.formError = 'Greška pri čuvanju modela';
          this.saving = false;
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/implementation-models']);
  }
}
