import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Projekat } from '../models/projekat.model';

@Injectable({
  providedIn: 'root'
})
export class ProjekatService {
  private apiUrl = `${environment.apiUrl}/projekti`;

  constructor(private http: HttpClient) { }

  getAll(createdByMe: boolean = true): Observable<Projekat[]> {
    return this.http.get<Projekat[]>(`${this.apiUrl}?createdByMe=${createdByMe}`);
  }

  getById(id: number): Observable<Projekat> {
    return this.http.get<Projekat>(`${this.apiUrl}/${id}`);
  }

  create(projekat: Projekat): Observable<Projekat> {
    return this.http.post<Projekat>(this.apiUrl, projekat);
  }

  update(id: number, projekat: Projekat): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, projekat);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
