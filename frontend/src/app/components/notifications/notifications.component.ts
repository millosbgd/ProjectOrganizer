import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { NotificationService } from '../../services/notification.service';
import { Notification } from '../../models/notification.model';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.css']
})
export class NotificationsComponent implements OnInit, OnDestroy {
  notifications: Notification[] = [];
  unreadCount = 0;
  loading = false;
  isOpen = false;

  private subscriptions = new Subscription();

  constructor(private notificationService: NotificationService) {}

  @HostListener('document:click')
  onDocumentClick(): void {
    this.isOpen = false;
  }

  ngOnInit(): void {
    this.notificationService.getUnreadCount().subscribe(count => {
      this.notificationService.setUnreadCount(count);
    });

    this.subscriptions.add(
      this.notificationService.unreadCount$.subscribe(count => {
        this.unreadCount = count;
      })
    );

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

  togglePanel(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen && this.notifications.length === 0) {
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
