import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserSettingsService } from '../../services/user-settings.service';
import { UserSettings, UpdateUserSettings } from '../../models/user-settings.model';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {
  // Settings loaded from backend (with masked keys)
  settings: UserSettings | null = null;
  
  // New values that user wants to set (only populated when user enters them)
  newOpenAiApiKey: string = '';
  newDevOpsPat: string = '';
  selectedModel: string = 'gpt-4o-mini';
  
  loading = false;
  saveSuccess = false;
  saveError = false;
  errorMessage = '';

  constructor(private settingsService: UserSettingsService) {}

  ngOnInit() {
    this.loadSettings();
  }

  loadSettings() {
    this.loading = true;
    this.settingsService.get().subscribe({
      next: (data) => {
        this.settings = data;
        this.selectedModel = data.openAiModel;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading settings:', error);
        this.errorMessage = 'Greška pri učitavanju podešavanja';
        this.loading = false;
      }
    });
  }

  saveSettings() {
    this.loading = true;
    this.saveSuccess = false;
    this.saveError = false;
    this.errorMessage = '';

    // Build update DTO - only include fields that user wants to change
    const updateDto: UpdateUserSettings = {
      openAiModel: this.selectedModel
    };

    // Only include API key if user entered a new one
    if (this.newOpenAiApiKey.trim()) {
      updateDto.openAiApiKey = this.newOpenAiApiKey.trim();
    }

    // Only include DevOps PAT if user entered a new one
    if (this.newDevOpsPat.trim()) {
      updateDto.devOpsPersonalAccessToken = this.newDevOpsPat.trim();
    }

    this.settingsService.update(updateDto).subscribe({
      next: () => {
        this.loading = false;
        this.saveSuccess = true;
        // Clear the input fields after successful save
        this.newOpenAiApiKey = '';
        this.newDevOpsPat = '';
        // Reload settings to get updated masked values
        this.loadSettings();
        setTimeout(() => this.saveSuccess = false, 3000);
      },
      error: (error) => {
        console.error('Error saving settings:', error);
        this.loading = false;
        this.saveError = true;
        this.errorMessage = error.error || 'Greška pri čuvanju podešavanja. Proverite format ključeva.';
        setTimeout(() => {
          this.saveError = false;
          this.errorMessage = '';
        }, 5000);
      }
    });
  }

  removeOpenAiKey() {
    if (!confirm('Da li ste sigurni da želite da uklonite OpenAI API ključ?')) {
      return;
    }

    this.loading = true;
    const updateDto: UpdateUserSettings = {
      openAiModel: this.selectedModel,
      openAiApiKey: 'REMOVE'
    };

    this.settingsService.update(updateDto).subscribe({
      next: () => {
        this.loading = false;
        this.saveSuccess = true;
        this.newOpenAiApiKey = '';
        this.loadSettings();
        setTimeout(() => this.saveSuccess = false, 3000);
      },
      error: (error) => {
        console.error('Error removing API key:', error);
        this.loading = false;
        this.saveError = true;
        this.errorMessage = 'Greška pri uklanjanju ključa';
        setTimeout(() => {
          this.saveError = false;
          this.errorMessage = '';
        }, 3000);
      }
    });
  }

  removeDevOpsPat() {
    if (!confirm('Da li ste sigurni da želite da uklonite DevOps PAT?')) {
      return;
    }

    this.loading = true;
    const updateDto: UpdateUserSettings = {
      openAiModel: this.selectedModel,
      devOpsPersonalAccessToken: 'REMOVE'
    };

    this.settingsService.update(updateDto).subscribe({
      next: () => {
        this.loading = false;
        this.saveSuccess = true;
        this.newDevOpsPat = '';
        this.loadSettings();
        setTimeout(() => this.saveSuccess = false, 3000);
      },
      error: (error) => {
        console.error('Error removing DevOps PAT:', error);
        this.loading = false;
        this.saveError = true;
        this.errorMessage = 'Greška pri uklanjanju tokena';
        setTimeout(() => {
          this.saveError = false;
          this.errorMessage = '';
        }, 3000);
      }
    });
  }
}

