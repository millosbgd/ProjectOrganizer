import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ImplementationItem } from '../models/implementation-model.model';
import { CheckListItem, AddCheckListItemsDto } from '../models/checklist-item.model';

@Injectable({
  providedIn: 'root'
})
export class ImplementationItemService {
  private apiUrl = `${environment.apiUrl}/implementationitems`;

  constructor(private http: HttpClient) { }

  /**
   * Get implementation item by ID with check list items
   */
  getById(id: number): Observable<ImplementationItem> {
    return this.http.get<ImplementationItem>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get check list items for implementation item
   */
  getCheckLists(itemId: number): Observable<CheckListItem[]> {
    return this.http.get<CheckListItem[]>(`${this.apiUrl}/${itemId}/checklists`);
  }

  /**
   * Add check list items to implementation item
   */
  addCheckLists(itemId: number, dto: AddCheckListItemsDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/${itemId}/checklists`, dto);
  }

  /**
   * Remove check list item from implementation item
   */
  removeCheckList(itemId: number, linkId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${itemId}/checklists/${linkId}`);
  }
}
