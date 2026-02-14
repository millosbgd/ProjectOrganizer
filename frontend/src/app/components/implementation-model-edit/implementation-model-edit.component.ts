import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ImplementationModelService } from '../../services/implementation-model.service';
import { ImplementationModel, ImplementationItem } from '../../models/implementation-model.model';

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
  editingItem: ImplementationItem = { id: 0, implementationModelId: 0, naziv: '', detalji: '' };
  editingItemIndex: number | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private implementationModelService: ImplementationModelService
  ) { }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isNew = false;
      this.loading = true;
      this.loadModel(+id);
    }
  }

  loadModel(id: number): void {
    this.implementationModelService.getById(id).subscribe({
      next: (data) => {
        this.model = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading model:', error);
        alert('Greška pri učitavanju modela');
        this.router.navigate(['/implementation-models']);
      }
    });
  }

  addItem(): void {
    this.editingItem = {
      id: 0,
      implementationModelId: this.model.id,
      naziv: '',
      detalji: ''
    };
    this.editingItemIndex = null;
    this.showItemModal = true;
  }

  editItem(index: number): void {
    this.editingItemIndex = index;
    // Create a copy to avoid direct editing
    this.editingItem = { ...this.model.items[index] };
    this.showItemModal = true;
  }

  saveItemFromModal(): void {
    if (!this.editingItem.naziv || this.editingItem.naziv.trim() === '') {
      alert('Naziv je obavezan');
      return;
    }

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
    this.editingItem = { id: 0, implementationModelId: 0, naziv: '', detalji: '' };
    this.editingItemIndex = null;
  }

  removeItem(index: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete ovu stavku?')) {
      this.model.items.splice(index, 1);
    }
  }

  save(): void {
    if (!this.model.naziv || this.model.naziv.trim() === '') {
      alert('Naziv je obavezan');
      return;
    }

    // Validate items
    for (const item of this.model.items) {
      if (!item.naziv || item.naziv.trim() === '') {
        alert('Sve stavke moraju imati naziv');
        return;
      }
    }

    this.saving = true;

    if (this.isNew) {
      this.implementationModelService.create(this.model).subscribe({
        next: (createdModel) => {
          alert('Model uspešno kreiran!');
          // Update model with created data (including ID)
          this.model = createdModel;
          this.isNew = false;
          this.saving = false;
        },
        error: (error: any) => {
          console.error('Error saving model:', error);
          alert('Greška pri čuvanju modela');
          this.saving = false;
        }
      });
    } else {
      this.implementationModelService.update(this.model.id, this.model).subscribe({
        next: () => {
          alert('Model uspešno ažuriran!');
          this.saving = false;
          // Reload model to get fresh data
          this.loadModel(this.model.id);
        },
        error: (error: any) => {
          console.error('Error saving model:', error);
          alert('Greška pri čuvanju modela');
          this.saving = false;
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/implementation-models']);
  }
}
