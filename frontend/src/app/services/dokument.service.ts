import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Dokument } from '../models/dokument.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DokumentService {
  private apiUrl = `${environment.apiUrl}/dokumenti`;

  constructor(private http: HttpClient) {}

  getByProjekatId(projekatId: number): Observable<Dokument[]> {
    return this.http.get<Dokument[]>(`${this.apiUrl}/projekat/${projekatId}`);
  }

  uploadDokument(projekatId: number, file: File): Observable<Dokument> {
    const formData = new FormData();
    formData.append('file', file);
    
    return this.http.post<Dokument>(`${this.apiUrl}/upload/${projekatId}`, formData);
  }

  downloadDokument(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/download`, { 
      responseType: 'blob' 
    });
  }

  deleteDokument(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }
}
