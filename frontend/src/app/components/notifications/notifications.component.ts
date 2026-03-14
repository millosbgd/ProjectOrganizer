import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { BadgeModule } from 'primeng/badge';
import { ButtonModule } from 'primeng/button';
import { NotificationService } from '../../services/notification.service';
import { Notification } from '../../models/notification.model';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, RouterModule, OverlayPanelModule, BadgeModule, ButtonModule],
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.css']
})
export class NotificationsComponent implements OnInit, OnDestroy {
  notifications: Notification[] = [];
  unreadCount = 0;
  loading = false;

  private subscriptions = new Subscription();

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    // Učitaj inicijalni broj nepročitanih
    this.notificationService.getUnreadCount().subscribe(count => {
      this.notificationService.setUnreadCount(count);
    });

    // Prati promene broja nepročitanih
    this.subscriptions.add(
      this.notificationService.unreadCount$.subscribe(count => {
        this.unreadCount = count;
      })
    );

    // Dodaj novu real-time notifikaciju na vrh liste
    this.subscriptions.add(
      this.notificationService.newNotification$.subscribe(notification => {
        if (notification) {
          this.notifications = [notification, ...this.notifications];
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  onPanelOpen(): void {
    if (this.notifications.length === 0) {
      this.loadNotifications();
    }
  }

  loadNotifications(): void {
    this.loading = true;
    this.notificationService.getNotifications().subscribe({
      next: data => {
        this.notifications = data;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  markAsRead(notification: Notification, event: Event): void {
    event.stopPropagation();
    if (notification.isRead) return;

    this.notificationService.markAsRead(notification.id).subscribe(() => {
      notification.isRead = true;
      this.notificationService.setUnreadCount(Math.max(0, this.unreadCount - 1));
    });
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe(() => {
      this.notifications.forEach(n => n.isRead = true);
      this.notificationService.setUnreadCount(0);
    });
  }

  getTypeIcon(type: string): string {
    switch (type) {
      case 'Blocked':             return 'block';
      case 'StatusChange':        return 'swap_horiz';
      case 'Inactive':            return 'schedule';
      case 'DeadlineApproaching': return 'alarm';
      default:                    return 'notifications';
    }
  }

  getTypeClass(type: string): string {
    switch (type) {
      case 'Blocked':             return 'type-blocked';
      case 'DeadlineApproaching': return 'type-deadline';
      case 'Inactive':            return 'type-inactive';
      default:                    return 'type-default';
    }
  }
}
