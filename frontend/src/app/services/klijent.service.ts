import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Klijent } from '../models/klijent.model';

@Injectable({
  providedIn: 'root'
})
export class KlijentService {
  private apiUrl = `${environment.apiUrl}/klijenti`;

  constructor(private http: HttpClient) { }

  getAll(): Observable<Klijent[]> {
    return this.http.get<Klijent[]>(this.apiUrl);
  }

  getById(id: number): Observable<Klijent> {
    return this.http.get<Klijent>(`${this.apiUrl}/${id}`);
  }

  create(klijent: Klijent): Observable<Klijent> {
    return this.http.post<Klijent>(this.apiUrl, klijent);
  }

  update(id: number, klijent: Klijent): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, klijent);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
