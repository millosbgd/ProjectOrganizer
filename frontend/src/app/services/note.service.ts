import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Note } from '../models/note.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class NoteService {
  private apiUrl = `${environment.apiUrl}/notes`;

  constructor(private http: HttpClient) {}

  getByProjekatId(projekatId: number): Observable<Note[]> {
    return this.http.get<Note[]>(`${this.apiUrl}/projekat/${projekatId}`);
  }

  getById(id: number): Observable<Note> {
    return this.http.get<Note>(`${this.apiUrl}/${id}`);
  }

  create(note: Note): Observable<Note> {
    return this.http.post<Note>(this.apiUrl, note);
  }

  update(id: number, note: Note): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, note);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
