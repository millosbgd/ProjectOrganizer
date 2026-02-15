import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CheckListItem } from '../models/checklist-item.model';

@Injectable({
  providedIn: 'root'
})
export class CheckListItemService {
  private apiUrl = `${environment.apiUrl}/checklistitems`;

  constructor(private http: HttpClient) { }

  /**
   * Get all check list items (codebook)
   */
  getAll(): Observable<CheckListItem[]> {
    return this.http.get<CheckListItem[]>(this.apiUrl);
  }

  /**
   * Get check list item by ID
   */
  getById(id: number): Observable<CheckListItem> {
    return this.http.get<CheckListItem>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create new check list item
   */
  create(item: CheckListItem): Observable<CheckListItem> {
    return this.http.post<CheckListItem>(this.apiUrl, item);
  }

  /**
   * Update check list item
   */
  update(id: number, item: CheckListItem): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, item);
  }

  /**
   * Delete check list item
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
