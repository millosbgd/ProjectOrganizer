import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface ParsedTask {
  orderIndex: number;
  title: string;
  description?: string;
  acceptanceCriteria?: string;
  priority?: string;
  estimation?: string;
  selected?: boolean;
}

@Component({
  selector: 'app-devops-tasks-preview-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './devops-tasks-preview-modal.component.html',
  styleUrls: ['./devops-tasks-preview-modal.component.css']
})
export class DevOpsTasksPreviewModalComponent {
  @Input() tasks: ParsedTask[] = [];
  @Input() aktivnostId: number = 0;
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<ParsedTask[]>();

  saving = false;

  get selectedCount(): number {
    return this.tasks.filter(t => t.selected).length;
  }

  get allSelected(): boolean {
    return this.tasks.length > 0 && this.tasks.every(t => t.selected);
  }

  toggleSelectAll(): void {
    const newState = !this.allSelected;
    this.tasks.forEach(t => t.selected = newState);
  }

  closeModal(): void {
    this.close.emit();
  }

  saveSelected(): void {
    const selectedTasks = this.tasks.filter(t => t.selected);
    if (selectedTasks.length === 0) {
      alert('Niste selektovali ni jedan task.');
      return;
    }
    
    this.save.emit(selectedTasks);
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
