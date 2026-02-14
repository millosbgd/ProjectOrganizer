import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CodebookEntry {
  code: string;
  value: string;
}

export interface CodebookEntityEntry {
  id: number;
  name: string;
  description?: string;
}

@Injectable({
  providedIn: 'root'
})
export class CodebookService {
  private apiUrl = `${environment.apiUrl}/codebooks`;

  constructor(private http: HttpClient) { }

  // Get all codebook entities (types)
  getEntities(): Observable<CodebookEntityEntry[]> {
    return this.http.get<CodebookEntityEntry[]>(`${this.apiUrl}/entities`);
  }

  // Get codebooks by entity type ID
  getByEntityType(entityTypeId: number): Observable<CodebookEntry[]> {
    return this.http.get<CodebookEntry[]>(`${this.apiUrl}/entity/${entityTypeId}`);
  }

  // Get codebooks by entity name (backward compatibility)
  getByEntityName(entityName: string): Observable<CodebookEntry[]> {
    return this.http.get<CodebookEntry[]>(`${this.apiUrl}/entityname/${entityName}`);
  }

  // Deprecated: Use getByEntityName instead
  getByType(type: string): Observable<CodebookEntry[]> {
    return this.getByEntityName(type);
  }

  // Deprecated: Use getEntities instead
  getTypes(): Observable<string[]> {
    // Transform entities to string array for backward compatibility
    return new Observable(observer => {
      this.getEntities().subscribe({
        next: (entities) => {
          observer.next(entities.map(e => e.name));
          observer.complete();
        },
        error: (err) => observer.error(err)
      });
    });
  }
}
