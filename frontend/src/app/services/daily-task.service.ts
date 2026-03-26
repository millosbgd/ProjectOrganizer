import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { DailyTask } from '../models/daily-task.model';

@Injectable({
  providedIn: 'root'
})
export class DailyTaskService {
  private readonly apiUrl = `${environment.apiUrl}/dailytasks`;

  constructor(private http: HttpClient) {}

  getTasks(): Observable<DailyTask[]> {
    return this.http.get<DailyTask[]>(this.apiUrl);
  }

  createTask(opis: string): Observable<DailyTask> {
    return this.http.post<DailyTask>(this.apiUrl, { opis });
  }

  toggleSolved(id: number): Observable<DailyTask> {
    return this.http.put<DailyTask>(`${this.apiUrl}/${id}/solved`, {});
  }

  togglePinned(id: number): Observable<DailyTask> {
    return this.http.put<DailyTask>(`${this.apiUrl}/${id}/pinned`, {});
  }

  deleteTask(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
