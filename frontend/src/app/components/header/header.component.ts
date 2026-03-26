import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { NotificationsComponent } from '../notifications/notifications.component';
import { NotificationService } from '../../services/notification.service';
import { DailyTasksComponent } from '../daily-tasks/daily-tasks.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, NotificationsComponent, DailyTasksComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent {
  constructor(public auth: AuthService, private notificationService: NotificationService) {
    // SignalR se pokreće odloženo (nakon inicijalnog rendera) da ne blokira first paint
    this.auth.isAuthenticated$.subscribe(isAuth => {
      if (isAuth) {
        setTimeout(() => this.notificationService.startConnection(), 500);
      } else {
        this.notificationService.stopConnection();
      }
    });
  }

  login(): void {
    this.auth.loginWithRedirect();
  }

  logout(): void {
    this.auth.logout({ 
      logoutParams: { 
        returnTo: window.location.origin 
      } 
    });
  }
}
