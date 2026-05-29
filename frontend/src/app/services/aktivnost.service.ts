import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Aktivnost } from '../models/aktivnost.model';

export interface BauBatchCreateRow {
  klijentId: number | null;
  bauTipAktivnosti: string;
  trajanjeMinuta: number | null;
  detalji?: string;
}

export interface BauBatchCreateRequest {
  datum: string;
  rows: BauBatchCreateRow[];
}

export interface BauBatchCreateResult {
  count: number;
  activityIds: number[];
}

@Injectable({
  providedIn: 'root'
})
export class AktivnostService {
  private apiUrl = `${environment.apiUrl}/aktivnosti`;

  constructor(private http: HttpClient) { }

  getAll(myActivitiesOnly: boolean = false): Observable<Aktivnost[]> {
    const params = myActivitiesOnly ? '?myActivitiesOnly=true' : '';
    return this.http.get<Aktivnost[]>(`${this.apiUrl}${params}`);
  }

  getById(id: number): Observable<Aktivnost> {
    return this.http.get<Aktivnost>(`${this.apiUrl}/${id}`);
  }

  getByProjekatId(projekatId: number): Observable<Aktivnost[]> {
    return this.http.get<Aktivnost[]>(`${this.apiUrl}?projekatId=${projekatId}`);
  }

  create(aktivnost: Aktivnost): Observable<Aktivnost> {
    return this.http.post<Aktivnost>(this.apiUrl, aktivnost);
  }

  createBauBatch(request: BauBatchCreateRequest): Observable<BauBatchCreateResult> {
    return this.http.post<BauBatchCreateResult>(`${this.apiUrl}/bau-batch`, request);
  }

  update(id: number, aktivnost: Aktivnost): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, aktivnost);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  generateZapisnik(id: number): Observable<string> {
    return this.http.post(`${this.apiUrl}/${id}/generate-zapisnik`, {}, { responseType: 'text' });
  }

  generateDevOpsTasks(id: number): Observable<string> {
    return this.http.post(`${this.apiUrl}/${id}/generate-devops-tasks`, {}, { responseType: 'text' });
  }

  parseDevOpsTasks(id: number, tasksText: string): Observable<any[]> {
    return this.http.post<any[]>(`${this.apiUrl}/${id}/parse-devops-tasks`, { tasksText });
  }

  saveSelectedTasks(id: number, tasks: any[]): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/save-selected-tasks`, { tasks });
  }

  generateReport(aktivnosti: Aktivnost[]): Observable<string> {
    return this.http.post(`${this.apiUrl}/generate-report`, aktivnosti, { responseType: 'text' });
  }

  generateOffer(aktivnostIds: number[]): Observable<string> {
    return this.http.post(`${this.apiUrl}/generate-offer`, { aktivnostIds }, { responseType: 'text' });
  }
}
