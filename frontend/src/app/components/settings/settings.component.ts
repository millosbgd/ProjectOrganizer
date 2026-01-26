import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserSettingsService } from '../../services/user-settings.service';
import { UserSettings } from '../../models/user-settings.model';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {
  settings: UserSettings = {
    id: 0,
    userId: '',
    openAiApiKey: '',
    openAiModel: 'gpt-4o-mini',
    createdAt: new Date(),
    updatedAt: new Date()
  };
  
  loading = false;
  saveSuccess = false;
  saveError = false;
  showApiKey = false;

  constructor(private settingsService: UserSettingsService) {}

  ngOnInit() {
    this.loadSettings();
  }

  loadSettings() {
    this.loading = true;
    this.settingsService.get().subscribe({
      next: (data) => {
        this.settings = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading settings:', error);
        this.loading = false;
      }
    });
  }

  saveSettings() {
    this.loading = true;
    this.saveSuccess = false;
    this.saveError = false;

    this.settingsService.update(this.settings).subscribe({
      next: () => {
        this.loading = false;
        this.saveSuccess = true;
        setTimeout(() => this.saveSuccess = false, 3000);
      },
      error: (error) => {
        console.error('Error saving settings:', error);
        this.loading = false;
        this.saveError = true;
        setTimeout(() => this.saveError = false, 3000);
      }
    });
  }

  toggleApiKeyVisibility() {
    this.showApiKey = !this.showApiKey;
  }
}
