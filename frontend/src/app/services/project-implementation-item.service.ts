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
    return this.http.put<void>(`${this.apiUrl}/${itemId}/checklist/${checklistId}`, {
      zavrsen: zavrsen,
      zavrsenDatum: zavrsen ? new Date().toISOString() : null
    });
  }

  updateCheckListKlijentPotvrdio(itemId: number, checklistId: number, klijentPotvrdio: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${itemId}/checklist/${checklistId}`, {
      klijentPotvrdio: klijentPotvrdio,
      klijentPotvrdioDatum: klijentPotvrdio ? new Date().toISOString() : null
    });
  }
}
