import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DevOpsTasksCandidateService } from '../../services/devops-tasks-candidate.service';
import { DevOpsTasksCandidate } from '../../models/devops-tasks-candidate.model';
import { DevOpsTaskEditModalComponent } from '../devops-task-edit-modal/devops-task-edit-modal.component';

@Component({
  selector: 'app-devops-tasks-modal',
  standalone: true,
  imports: [CommonModule, DevOpsTaskEditModalComponent],
  templateUrl: './devops-tasks-modal.component.html',
  styleUrls: ['./devops-tasks-modal.component.css']
})
export class DevOpsTasksModalComponent implements OnInit {
  @Input() aktivnostId: number = 0;
  @Output() close = new EventEmitter<void>();

  tasks: DevOpsTasksCandidate[] = [];
  loading = true;
  selectedTasks: Set<number> = new Set();
  
  // Edit modal
  isEditModalOpen = false;
  taskToEdit: DevOpsTasksCandidate | null = null;

  constructor(private devOpsTasksCandidateService: DevOpsTasksCandidateService) {}

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading = true;
    this.devOpsTasksCandidateService.getCandidatesForAktivnost(this.aktivnostId).subscribe({
      next: (data) => {
        this.tasks = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading tasks:', error);
        this.loading = false;
      }
    });
  }

  closeModal(): void {
    this.close.emit();
  }

  toggleTaskSelection(taskId: number): void {
    if (this.selectedTasks.has(taskId)) {
      this.selectedTasks.delete(taskId);
    } else {
      this.selectedTasks.add(taskId);
    }
  }

  isTaskSelected(taskId: number): boolean {
    return this.selectedTasks.has(taskId);
  }

  editTask(task: DevOpsTasksCandidate): void {
    this.taskToEdit = { ...task };
    this.isEditModalOpen = true;
  }

  closeEditModal(): void {
    this.isEditModalOpen = false;
    this.taskToEdit = null;
  }

  saveTask(task: DevOpsTasksCandidate): void {
    if (task.id) {
      this.devOpsTasksCandidateService.updateCandidate(task.id, {
        title: task.title,
        description: task.description,
        acceptanceCriteria: task.acceptanceCriteria,
        priority: task.priority,
        estimation: task.estimation,
        orderIndex: task.orderIndex,
        status: task.status
      }).subscribe({
        next: () => {
          this.closeEditModal();
          this.loadTasks();
        },
        error: (error) => {
          console.error('Error updating task:', error);
          alert('Greška prilikom ažuriranja taska.');
        }
      });
    }
  }

  deleteTask(taskId: number): void {
    if (confirm('Da li ste sigurni da želite da obrišete ovaj task?')) {
      this.devOpsTasksCandidateService.deleteCandidate(taskId).subscribe({
        next: () => {
          this.loadTasks();
        },
        error: (error) => {
          console.error('Error deleting task:', error);
          alert('Greška prilikom brisanja taska.');
        }
      });
    }
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Draft':
        return 'badge-draft';
      case 'Sent':
        return 'badge-sent';
      case 'Rejected':
        return 'badge-rejected';
      default:
        return 'badge-draft';
    }
  }

  getPriorityBadgeClass(priority: string | undefined): string {
    if (!priority) return 'badge-medium';
    
    switch (priority.toLowerCase()) {
      case 'high':
        return 'badge-high';
      case 'medium':
        return 'badge-medium';
      case 'low':
        return 'badge-low';
      default:
        return 'badge-medium';
    }
  }
}
