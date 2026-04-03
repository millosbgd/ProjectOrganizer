import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class GoogleSheetsService {
  private apiUrl = `${environment.apiUrl}/googlesheets`;

  constructor(private http: HttpClient) {}

  generateSheet(projekatId: number): Observable<{ spreadsheetId: string; url: string }> {
    return this.http.post<{ spreadsheetId: string; url: string }>(
      `${this.apiUrl}/projekat/${projekatId}/generate`, {}
    );
  }

  syncFromSheet(projekatId: number): Observable<{ synced: number }> {
    return this.http.post<{ synced: number }>(
      `${this.apiUrl}/projekat/${projekatId}/sync`, {}
    );
  }

  generateInternalSheet(projekatId: number): Observable<{ spreadsheetId: string; url: string }> {
    return this.http.post<{ spreadsheetId: string; url: string }>(
      `${this.apiUrl}/projekat/${projekatId}/generate-internal`, {}
    );
  }
}
