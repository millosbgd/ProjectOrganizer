import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ProjectPermission } from '../models/project-permission.model';

@Injectable({
  providedIn: 'root'
})
export class ProjectPermissionService {
  private apiUrl = `${environment.apiUrl}/projectpermissions`;

  constructor(private http: HttpClient) {}

  getPermissionsForProjekat(projekatId: number): Observable<ProjectPermission[]> {
    return this.http.get<ProjectPermission[]>(`${this.apiUrl}/projekat/${projekatId}`);
  }

  addPermission(permission: ProjectPermission): Observable<ProjectPermission> {
    return this.http.post<ProjectPermission>(this.apiUrl, permission);
  }

  updatePermission(id: number, permission: ProjectPermission): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, permission);
  }

  deletePermission(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getMyProjects(): Observable<number[]> {
    return this.http.get<number[]>(`${this.apiUrl}/my-projects`);
  }
}
