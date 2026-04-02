import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MailService } from '../../services/mail.service';
import { MsalMailService } from '../../services/msal-mail.service';
import { Mail } from '../../models/mail.model';

@Component({
  selector: 'app-mails',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mails.component.html',
  styleUrls: ['./mails.component.css']
})
export class MailsComponent implements OnInit {
  mails: Mail[] = [];
  loading = false;
  fetching = false;
  days = 7;

  loggedInAccount: string | null = null;

  showToast = false;
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  selectedMail: Mail | null = null;

  constructor(
    private mailService: MailService,
    private msalMailService: MsalMailService
  ) {}

  ngOnInit(): void {
    this.loadMails();
    this.msalMailService.initialize().then(() => {
      this.loggedInAccount = this.msalMailService.getLoggedInAccount();
    });
  }

  loadMails(): void {
    this.loading = true;
    this.mailService.getAll().subscribe({
      next: (data) => {
        this.mails = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  async fetchMails(): Promise<void> {
    this.fetching = true;
    try {
      const accessToken = await this.msalMailService.getAccessToken();
      this.loggedInAccount = this.msalMailService.getLoggedInAccount();

      this.mailService.fetchMails({ accessToken, days: this.days }).subscribe({
        next: (result) => {
          this.fetching = false;
          this.showToastMessage(`Učitano ${result.newCount} novih mailova (ukupno pronađeno: ${result.totalFetched})`, 'success');
          this.loadMails();
        },
        error: (err) => {
          this.fetching = false;
          this.showToastMessage('Greška pri učitavanju mailova', 'error');
        }
      });
    } catch (err: any) {
      this.fetching = false;
      if (err?.errorCode === 'user_cancelled') {
        this.showToastMessage('Prijava otkazana', 'error');
      } else {
        this.showToastMessage('Greška pri prijavi na Microsoft nalog', 'error');
      }
    }
  }

  async logoutMicrosoft(): Promise<void> {
    await this.msalMailService.logout();
    this.loggedInAccount = null;
  }

  openMail(mail: Mail): void {
    this.selectedMail = mail;
  }

  closeMail(): void {
    this.selectedMail = null;
  }

  deleteMail(id: number, event: Event): void {
    event.stopPropagation();
    this.mailService.delete(id).subscribe({
      next: () => {
        this.mails = this.mails.filter(m => m.id !== id);
        if (this.selectedMail?.id === id) this.selectedMail = null;
      },
      error: () => {
        this.showToastMessage('Greška pri brisanju maila', 'error');
      }
    });
  }

  showToastMessage(message: string, type: 'success' | 'error'): void {
    this.toastMessage = message;
    this.toastType = type;
    this.showToast = true;
    setTimeout(() => { this.showToast = false; }, 3500);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toLocaleDateString('sr-Latn-RS', { day: '2-digit', month: '2-digit', year: 'numeric' })
      + ' ' + d.toLocaleTimeString('sr-Latn-RS', { hour: '2-digit', minute: '2-digit' });
  }
}
