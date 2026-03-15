import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '@auth0/auth0-angular';
import { environment } from '../../environments/environment';
import { Notification } from '../models/notification.model';

@Injectable({
  providedIn: 'root'
})
export class NotificationService implements OnDestroy {
  private readonly apiUrl = `${environment.apiUrl}/notifications`;
  // Hub URL je isti host kao API, bez /api sufiksa
  private readonly hubUrl = environment.apiUrl.replace('/api', '');

  private hubConnection?: signalR.HubConnection;

  private unreadCountSubject    = new BehaviorSubject<number>(0);
  private newNotificationSubject = new BehaviorSubject<Notification | null>(null);

  /** Emituje ukupan broj nepročitanih notifikacija */
  unreadCount$     = this.unreadCountSubject.asObservable();
  /** Emituje svaku novu notifikaciju primljenu u realnom vremenu */
  newNotification$ = this.newNotificationSubject.asObservable();

  constructor(private http: HttpClient, private auth: AuthService) {}

  // ----------------------------------------------------------------
  // SignalR konekcija
  // ----------------------------------------------------------------

  /** Poziva se jednom po prijavi korisnika (npr. u AppComponent). */
  startConnection(): void {
    this.auth.getAccessTokenSilently().subscribe({
      next: token => {
        this.hubConnection = new signalR.HubConnectionBuilder()
          .withUrl(`${this.hubUrl}/hubs/notifications`, {
            accessTokenFactory: () => token
          })
          .withAutomaticReconnect()
          .configureLogging(signalR.LogLevel.Warning)
          .build();

        this.hubConnection.on('ReceiveNotification', (notification: Notification) => {
          this.newNotificationSubject.next(notification);
          this.unreadCountSubject.next(this.unreadCountSubject.value + 1);
        });

        this.hubConnection
          .start()
          .catch(err => console.error('[SignalR] Connection error:', err));
      },
      error: err => console.error('[SignalR] Token error:', err)
    });
  }

  stopConnection(): void {
    this.hubConnection?.stop();
  }

  ngOnDestroy(): void {
    this.stopConnection();
  }

  // ----------------------------------------------------------------
  // HTTP metode
  // ----------------------------------------------------------------

  getNotifications(page = 1, pageSize = 20): Observable<Notification[]> {
    return this.http.get<Notification[]>(`${this.apiUrl}?page=${page}&pageSize=${pageSize}`);
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/unread-count`);
  }

  markAsRead(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/read`, {});
  }

  dismiss(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/dismiss`, {});
  }

  markAllAsRead(): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/read-all`, {});
  }

  // Koristi se pri inicijalnom učitavanju da se podesi početni broj
  setUnreadCount(count: number): void {
    this.unreadCountSubject.next(count);
  }
}
