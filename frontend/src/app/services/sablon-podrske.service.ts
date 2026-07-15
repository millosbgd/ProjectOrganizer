import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SablonPodrske } from '../models/sablon-podrske.model';

@Injectable({
  providedIn: 'root'
})
export class SablonPodrskeService {
  private apiUrl = `${environment.apiUrl}/sablonipodrske`;

  constructor(private http: HttpClient) { }

  getAll(myTemplatesOnly: boolean = true): Observable<SablonPodrske[]> {
    return this.http.get<SablonPodrske[]>(`${this.apiUrl}?myTemplatesOnly=${myTemplatesOnly}`);
  }

  getById(id: number): Observable<SablonPodrske> {
    return this.http.get<SablonPodrske>(`${this.apiUrl}/${id}`);
  }

  create(sablon: SablonPodrske): Observable<SablonPodrske> {
    return this.http.post<SablonPodrske>(this.apiUrl, sablon);
  }

  update(id: number, sablon: SablonPodrske): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, sablon);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
