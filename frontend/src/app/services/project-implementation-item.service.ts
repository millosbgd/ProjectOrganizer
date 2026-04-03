import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ProjectImplementationItem, ProjectImplementationCheckListItem } from '../models/project-implementation-item.model';

@Injectable({
  providedIn: 'root'
})
export class ProjectImplementationItemService {
  private apiUrl = `${environment.apiUrl}/ProjectImplementationItems`;

  constructor(private http: HttpClient) { }

  getByProjectId(projectId: number): Observable<ProjectImplementationItem[]> {
    return this.http.get<ProjectImplementationItem[]>(`${this.apiUrl}/project/${projectId}`);
  }

  getById(id: number): Observable<ProjectImplementationItem> {
    return this.http.get<ProjectImplementationItem>(`${this.apiUrl}/${id}`);
  }

  update(id: number, item: ProjectImplementationItem): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, item);
  }

  updateCheckList(itemId: number, checklistId: number, zavrsen: boolean): Observable<void> {
    const today = new Date().toISOString().split('T')[0];
    return this.http.put<void>(`${this.apiUrl}/${itemId}/checklist/${checklistId}`, {
      zavrsen: zavrsen,
      zavrsenDatum: zavrsen ? today : null
    });
  }

  updateCheckListKlijentPotvrdio(itemId: number, checklistId: number, klijentPotvrdio: boolean): Observable<void> {
    const today = new Date().toISOString().split('T')[0];
    return this.http.put<void>(`${this.apiUrl}/${itemId}/checklist/${checklistId}`, {
      klijentPotvrdio: klijentPotvrdio,
      klijentPotvrdioDatum: klijentPotvrdio ? today : null
    });
  }

  updateCheckListFields(itemId: number, checklistId: number, fields: { detaljanOpis?: string | null; planiraniRok?: string | null; clearPlaniraniRok?: boolean; kompleksnost?: number | null; opis?: string | null }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${itemId}/checklist/${checklistId}`, fields);
  }

  addCustomCheckListItem(itemId: number, dto: { opis: string; detaljanOpis?: string | null; planiraniRok?: string | null; kompleksnost?: number | null }): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${itemId}/checklist`, dto);
  }

  deleteCustomCheckListItem(itemId: number, checklistId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${itemId}/checklist/${checklistId}`);
  }
}
