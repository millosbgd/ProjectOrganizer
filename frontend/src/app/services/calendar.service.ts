import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CalendarActivity, UpdateActivityTimeDto } from '../models/calendar-activity.model';

export interface ScheduleBauDayActivity {
  id: number;
  startUtc: string;
  endUtc: string;
  requestedDurationMinutes: number;
  durationMinutes: number;
}

export interface ScheduleBauDayResult {
  scheduledCount: number;
  totalBauMinutes: number;
  scheduledBauMinutes: number;
  wasScaled: boolean;
  fixedActivityCount: number;
  activities: ScheduleBauDayActivity[];
}

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

  /**
   * Automatically schedule BAU activities for one local day into the 08:00-16:00 window.
   * Existing non-BAU activities for that day stay fixed.
   */
  scheduleBauDay(datum: string): Observable<ScheduleBauDayResult> {
    return this.http.post<ScheduleBauDayResult>(`${this.apiUrl}/schedule-bau-day`, { datum });
  }
}
