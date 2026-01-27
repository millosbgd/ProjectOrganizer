import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CodebookEntry {
  code: string;
  value: string;
}

@Injectable({
  providedIn: 'root'
})
export class CodebookService {
  private apiUrl = `${environment.apiUrl}/codebooks`;

  constructor(private http: HttpClient) { }

  getByType(type: string): Observable<CodebookEntry[]> {
    return this.http.get<CodebookEntry[]>(`${this.apiUrl}/${type}`);
  }

  getTypes(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/types`);
  }
}
