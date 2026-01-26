import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserSettings } from '../models/user-settings.model';

@Injectable({
  providedIn: 'root'
})
export class UserSettingsService {
  private apiUrl = `${environment.apiUrl}/usersettings`;

  constructor(private http: HttpClient) { }

  get(): Observable<UserSettings> {
    return this.http.get<UserSettings>(this.apiUrl);
  }

  update(settings: UserSettings): Observable<void> {
    return this.http.put<void>(this.apiUrl, settings);
  }
}
