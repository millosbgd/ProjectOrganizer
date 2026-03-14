import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { NotificationsComponent } from '../notifications/notifications.component';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, NotificationsComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent {
  constructor(public auth: AuthService, private notificationService: NotificationService) {
    // Pokretanje SignalR konekcije kada je korisnik ulogovan
    this.auth.isAuthenticated$.subscribe(isAuth => {
      if (isAuth) {
        this.notificationService.startConnection();
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
