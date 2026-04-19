import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { DevOpsTasksCandidate, FetchedDevOpsTask } from '../models/devops-tasks-candidate.model';

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

  createCandidate(data: {
    aktivnostId: number;
    title: string;
    description?: string;
    acceptanceCriteria?: string;
    priority?: string;
    estimation?: string;
    orderIndex?: number;
    devOpsWorkItemId?: number;
    devOpsUrl?: string;
  }): Observable<{ id: number; aktivnostId: number; title: string; status: string; createdAt: Date }> {
    return this.http.post<any>(this.apiUrl, { orderIndex: 0, ...data });
  }

  fetchFromDevOpsUrl(url: string): Observable<FetchedDevOpsTask> {
    return this.http.post<FetchedDevOpsTask>(`${this.apiUrl}/fetch-from-url`, { url });
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
