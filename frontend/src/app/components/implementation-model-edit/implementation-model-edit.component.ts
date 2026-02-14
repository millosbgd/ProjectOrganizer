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
    const newItem: ImplementationItem = {
      id: 0,
      implementationModelId: this.model.id,
      naziv: '',
      detalji: ''
    };
    this.model.items.push(newItem);
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
        next: () => {
          this.router.navigate(['/implementation-models']);
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
          this.router.navigate(['/implementation-models']);
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
