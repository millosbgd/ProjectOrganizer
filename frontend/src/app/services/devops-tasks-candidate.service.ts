import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { DevOpsTasksCandidate } from '../models/devops-tasks-candidate.model';

@Injectable({
  providedIn: 'root'
})
export class DevOpsTasksCandidateService {
  private apiUrl = `${environment.apiUrl}/devopstaskscandidates`;

  constructor(private http: HttpClient) {}

  getCandidatesForAktivnost(aktivnostId: number): Observable<DevOpsTasksCandidate[]> {
    return this.http.get<DevOpsTasksCandidate[]>(`${this.apiUrl}/aktivnost/${aktivnostId}`);
  }

  getCandidate(id: number): Observable<DevOpsTasksCandidate> {
    return this.http.get<DevOpsTasksCandidate>(`${this.apiUrl}/${id}`);
  }

  updateCandidate(id: number, candidate: Partial<DevOpsTasksCandidate>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, candidate);
  }

  updateStatus(id: number, status: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/status`, { status });
  }

  deleteCandidate(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
