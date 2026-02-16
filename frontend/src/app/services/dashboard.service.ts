import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { DashboardStats } from '../models/dashboard.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = `${environment.apiUrl}/dashboard`;

  constructor(private http: HttpClient) { }

  /**
   * Dohvata sve dashboard statistike
   */
  getStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.apiUrl}/stats`);
  }

  /**
   * Dohvata samo osnovne metrike (brojeve)
   */
  getBasicMetrics(): Observable<{activeProjectsCount: number, unfinishedActivitiesCount: number}> {
    return this.http.get<{activeProjectsCount: number, unfinishedActivitiesCount: number}>(`${this.apiUrl}/metrics`);
  }
}
