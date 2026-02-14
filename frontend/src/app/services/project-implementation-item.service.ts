import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ProjectImplementationItem } from '../models/project-implementation-item.model';

@Injectable({
  providedIn: 'root'
})
export class ProjectImplementationItemService {
  private apiUrl = `${environment.apiUrl}/ProjectImplementationItems`;

  constructor(private http: HttpClient) { }

  getByProjectId(projectId: number): Observable<ProjectImplementationItem[]> {
    return this.http.get<ProjectImplementationItem[]>(`${this.apiUrl}/project/${projectId}`);
  }

  update(id: number, item: ProjectImplementationItem): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, item);
  }
}
