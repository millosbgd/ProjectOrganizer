import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DevOpsTasksCandidate, StatusHistoryEntry } from '../../models/devops-tasks-candidate.model';
import { DevOpsTasksCandidateService } from '../../services/devops-tasks-candidate.service';

@Component({
  selector: 'app-devops-task-edit-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './devops-task-edit-modal.component.html',
  styleUrl: './devops-task-edit-modal.component.css'
})
export class DevOpsTaskEditModalComponent {
  @Input() task: DevOpsTasksCandidate | null = null;
  @Input() isOpen = false;
  @Output() save = new EventEmitter<DevOpsTasksCandidate>();
  @Output() close = new EventEmitter<void>();

  refreshLoading = false;
  refreshError: string | null = null;

  statusOptions = [
    { value: 'Draft', label: 'Draft' },
    { value: 'Sent', label: 'Sent' },
    { value: 'Rejected', label: 'Rejected' }
  ];

  constructor(private devOpsTasksCandidateService: DevOpsTasksCandidateService) {}

  refreshFromDevOps(): void {
    if (!this.task?.devOpsUrl) return;

    this.refreshLoading = true;
    this.refreshError = null;

    this.devOpsTasksCandidateService.fetchFromDevOpsUrl(this.task.devOpsUrl, this.task.id).subscribe({
      next: (fetched) => {
        this.task!.title = fetched.title ?? this.task!.title;
        this.task!.description = fetched.description ?? this.task!.description;
        this.task!.acceptanceCriteria = fetched.acceptanceCriteria ?? this.task!.acceptanceCriteria;
        this.task!.priority = fetched.priority ?? this.task!.priority;
        this.task!.estimation = fetched.estimation ?? this.task!.estimation;
        if (fetched.statusHistory && fetched.statusHistory.length > 0) {
          this.task!.statusHistory = fetched.statusHistory;
        }
        this.refreshLoading = false;
      },
      error: (err) => {
        this.refreshError = err?.error?.message ?? 'Greška pri osvežavanju iz DevOps-a.';
        this.refreshLoading = false;
      }
    });
  }

  onSave(): void {
    if (this.task) {
      if (!this.task.title || this.task.title.trim() === '') {
        alert('Naziv taska je obavezan.');
        return;
      }
      this.save.emit(this.task);
    }
  }

  onClose(): void {
    this.refreshError = null;
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  formatDuration(minutes?: number, isActive?: boolean): string {
    const base = minutes != null && minutes > 0 ? (() => {
      if (minutes < 60) return `${minutes} min`;
      const h = Math.floor(minutes / 60);
      const d = Math.floor(h / 24);
      if (d > 0) return `${d}d ${h % 24}h`;
      return `${h}h ${minutes % 60 > 0 ? (minutes % 60) + 'min' : ''}`.trim();
    })() : null;
    if (isActive) return base ? `${base} + trenutno aktivno` : 'trenutno aktivno';
    return base ?? '—';
  }
}
