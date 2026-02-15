import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CalendarActivity, UpdateActivityTimeDto } from '../models/calendar-activity.model';

@Injectable({
  providedIn: 'root'
})
export class CalendarService {
  private apiUrl = `${environment.apiUrl}/activities`;

  constructor(private http: HttpClient) { }

  /**
   * Get activities for a specific date range
   * @param from ISO string (UTC)
   * @param to ISO string (UTC)
   */
  getActivities(from: string, to: string): Observable<CalendarActivity[]> {
    const params = new HttpParams()
      .set('from', from)
      .set('to', to);
    
    return this.http.get<CalendarActivity[]>(this.apiUrl, { params });
  }

  /**
   * Update activity start and end time
   * @param id Activity ID
   * @param dto Update DTO with UTC timestamps
   */
  updateActivityTime(id: number, dto: UpdateActivityTimeDto): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/time`, dto);
  }
}
