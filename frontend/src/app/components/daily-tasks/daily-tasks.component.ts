import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DailyTaskService } from '../../services/daily-task.service';
import { DailyTask } from '../../models/daily-task.model';

@Component({
  selector: 'app-daily-tasks',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './daily-tasks.component.html',
  styleUrls: ['./daily-tasks.component.css']
})
export class DailyTasksComponent implements OnInit {
  tasks: DailyTask[] = [];
  loading = false;
  isOpen = false;
  showAddForm = false;
  newTaskOpis = '';
  saving = false;

  constructor(private dailyTaskService: DailyTaskService) {}

  @HostListener('document:click')
  onDocumentClick(): void {
    this.isOpen = false;
    this.showAddForm = false;
  }

  ngOnInit(): void {}

  togglePanel(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen && this.tasks.length === 0) {
      this.loadTasks();
    }
    if (!this.isOpen) {
      this.showAddForm = false;
    }
  }

  loadTasks(): void {
    this.loading = true;
    this.dailyTaskService.getTasks().subscribe({
      next: data => {
        this.tasks = data;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  toggleAddForm(event: Event): void {
    event.stopPropagation();
    this.showAddForm = !this.showAddForm;
    if (!this.showAddForm) {
      this.newTaskOpis = '';
    }
  }

  submitNewTask(event: Event): void {
    event.stopPropagation();
    const opis = this.newTaskOpis.trim();
    if (!opis) return;

    this.saving = true;
    this.dailyTaskService.createTask(opis).subscribe({
      next: task => {
        this.tasks = [task, ...this.tasks];
        this.newTaskOpis = '';
        this.showAddForm = false;
        this.saving = false;
      },
      error: () => { this.saving = false; }
    });
  }

  cancelAdd(event: Event): void {
    event.stopPropagation();
    this.newTaskOpis = '';
    this.showAddForm = false;
  }

  toggleSolved(task: DailyTask, event: Event): void {
    event.stopPropagation();
    this.dailyTaskService.toggleSolved(task.id).subscribe(updated => {
      task.solved = updated.solved;
    });
  }

  togglePinned(task: DailyTask, event: Event): void {
    event.stopPropagation();
    this.dailyTaskService.togglePinned(task.id).subscribe(updated => {
      task.pinned = updated.pinned;
      // Re-sort: pinned tasks first
      this.tasks = [
        ...this.tasks.filter(t => t.pinned),
        ...this.tasks.filter(t => !t.pinned)
      ];
    });
  }

  deleteTask(task: DailyTask, event: Event): void {
    event.stopPropagation();
    this.dailyTaskService.deleteTask(task.id).subscribe(() => {
      this.tasks = this.tasks.filter(t => t.id !== task.id);
    });
  }

  get pendingCount(): number {
    return this.tasks.filter(t => !t.solved).length;
  }
}
