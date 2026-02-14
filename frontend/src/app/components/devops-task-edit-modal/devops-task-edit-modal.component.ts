import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DevOpsTasksCandidate } from '../../models/devops-tasks-candidate.model';

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

  statusOptions = [
    { value: 'Draft', label: 'Draft' },
    { value: 'Sent', label: 'Sent' },
    { value: 'Rejected', label: 'Rejected' }
  ];

  onSave(): void {
    if (this.task) {
      // Validate required fields
      if (!this.task.title || this.task.title.trim() === '') {
        alert('Naziv taska je obavezan.');
        return;
      }
      this.save.emit(this.task);
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
}
