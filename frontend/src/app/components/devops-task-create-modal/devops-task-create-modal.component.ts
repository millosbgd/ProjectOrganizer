import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DevOpsTasksCandidateService } from '../../services/devops-tasks-candidate.service';
import { FetchedDevOpsTask } from '../../models/devops-tasks-candidate.model';

export interface NewDevOpsTaskForm {
  title: string;
  description?: string;
  acceptanceCriteria?: string;
  priority?: string;
  estimation?: string;
  devOpsWorkItemId?: number;
  devOpsUrl?: string;
}

@Component({
  selector: 'app-devops-task-create-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './devops-task-create-modal.component.html',
  styleUrls: ['./devops-task-create-modal.component.css']
})
export class DevOpsTaskCreateModalComponent implements OnInit {
  @Input() aktivnostId: number = 0;
  @Output() save = new EventEmitter<NewDevOpsTaskForm>();
  @Output() close = new EventEmitter<void>();

  devOpsUrl: string = '';
  fetchLoading: boolean = false;
  fetchError: string | null = null;
  workItemType: string | null = null;
  saving: boolean = false;

  form: NewDevOpsTaskForm = {
    title: '',
    description: '',
    acceptanceCriteria: '',
    priority: '',
    estimation: ''
  };

  constructor(private devOpsTasksCandidateService: DevOpsTasksCandidateService) {}

  ngOnInit(): void {}

  onUrlPaste(event: ClipboardEvent): void {
    const pasted = event.clipboardData?.getData('text')?.trim() ?? '';
    if (pasted && this.looksLikeDevOpsUrl(pasted)) {
      // Allow the paste to complete, then fetch
      setTimeout(() => this.fetchFromUrl(pasted), 0);
    }
  }

  onUrlChange(): void {
    this.fetchError = null;
    this.workItemType = null;
  }

  fetchFromUrl(urlOverride?: string): void {
    const url = (urlOverride ?? this.devOpsUrl).trim();
    if (!url) return;

    this.fetchLoading = true;
    this.fetchError = null;
    this.workItemType = null;

    this.devOpsTasksCandidateService.fetchFromDevOpsUrl(url).subscribe({
      next: (task: FetchedDevOpsTask) => {
        this.form.title = task.title ?? '';
        this.form.description = task.description ?? '';
        this.form.acceptanceCriteria = task.acceptanceCriteria ?? '';
        this.form.priority = task.priority ?? '';
        this.form.estimation = task.estimation ?? '';
        this.form.devOpsWorkItemId = task.devOpsWorkItemId;
        this.form.devOpsUrl = task.devOpsUrl ?? url;
        this.workItemType = task.workItemType ?? null;
        this.fetchLoading = false;
      },
      error: (err) => {
        this.fetchError = err?.error?.message ?? 'Greška pri učitavanju taska.';
        this.fetchLoading = false;
      }
    });
  }

  clearUrl(): void {
    this.devOpsUrl = '';
    this.fetchError = null;
    this.workItemType = null;
    this.form.devOpsWorkItemId = undefined;
    this.form.devOpsUrl = undefined;
  }

  onSave(): void {
    if (!this.form.title?.trim()) {
      return;
    }
    this.save.emit({ ...this.form });
  }

  onClose(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  private looksLikeDevOpsUrl(url: string): boolean {
    return url.includes('dev.azure.com') || url.includes('.visualstudio.com');
  }
}
