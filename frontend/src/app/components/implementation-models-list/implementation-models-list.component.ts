import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ImplementationModelService } from '../../services/implementation-model.service';
import { ImplementationModel } from '../../models/implementation-model.model';

@Component({
  selector: 'app-implementation-models-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './implementation-models-list.component.html',
  styleUrls: ['./implementation-models-list.component.css']
})
export class ImplementationModelsListComponent implements OnInit {
  models: ImplementationModel[] = [];
  loading = true;

  constructor(private implementationModelService: ImplementationModelService) { }

  ngOnInit(): void {
    this.loadModels();
  }

  loadModels(): void {
    this.implementationModelService.getAll().subscribe({
      next: (data) => {
        this.models = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading implementation models:', error);
        this.loading = false;
      }
    });
  }

  deleteModel(id: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete ovaj model?')) {
      this.implementationModelService.delete(id).subscribe({
        next: () => {
          this.loadModels();
        },
        error: (error) => {
          console.error('Error deleting model:', error);
        }
      });
    }
  }
}
