import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Mail, FetchMailsRequest, FetchMailsResult } from '../models/mail.model';

@Injectable({
  providedIn: 'root'
})
export class MailService {
  private apiUrl = `${environment.apiUrl}/mail`;

  constructor(private http: HttpClient) { }

  fetchMails(request: FetchMailsRequest): Observable<FetchMailsResult> {
    return this.http.post<FetchMailsResult>(`${this.apiUrl}/fetch`, request);
  }

  getAll(): Observable<Mail[]> {
    return this.http.get<Mail[]>(this.apiUrl);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
