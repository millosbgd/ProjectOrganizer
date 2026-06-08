import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { Subscription } from 'rxjs';
import { UserService } from '../../services/user.service';

interface SidebarMenuItem {
  key: string;
  route: string;
  icon: string;
  label: string;
}

@Component({
  selector: 'app-sidebar-nav',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar-nav.component.html',
  styleUrls: ['./sidebar-nav.component.css']
})
export class SidebarNavComponent implements OnDestroy {
  isCollapsed = false;
  visibleMenuItems: SidebarMenuItem[] = [];

  private readonly menuItems: SidebarMenuItem[] = [
    { key: 'dashboard', route: '/dashboard', icon: 'dashboard', label: 'Dashboard' },
    { key: 'projekti', route: '/projekti', icon: 'folder', label: 'Projekti' },
    { key: 'klijenti', route: '/klijenti', icon: 'people', label: 'Klijenti' },
    { key: 'aktivnosti', route: '/aktivnosti', icon: 'checklist', label: 'Aktivnosti' },
    { key: 'kalendar', route: '/kalendar', icon: 'calendar_today', label: 'Kalendar' },
    { key: 'posete-gorivo', route: '/posete-gorivo', icon: 'local_gas_station', label: 'Posete i gorivo' },
    { key: 'implementation-models', route: '/implementation-models', icon: 'build', label: 'Modeli Implementacije' },
    { key: 'admin-users', route: '/admin/users', icon: 'manage_accounts', label: 'Korisnici' },
    { key: 'settings', route: '/settings', icon: 'settings', label: 'Podešavanja' }
  ];

  private readonly subscription = new Subscription();

  constructor(public auth: AuthService, private userService: UserService) {
    // Load saved state from localStorage
    const savedState = localStorage.getItem('sidebarCollapsed');
    if (savedState !== null) {
      this.isCollapsed = savedState === 'true';
    }
    // Set initial body class
    this.updateBodyClass();

    this.subscription.add(
      this.auth.isAuthenticated$.subscribe(isAuthenticated => {
        if (isAuthenticated) {
          this.loadMenuPermissions();
        } else {
          this.visibleMenuItems = [];
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  toggleSidebar(): void {
    this.isCollapsed = !this.isCollapsed;
    // Save state to localStorage
    localStorage.setItem('sidebarCollapsed', this.isCollapsed.toString());
    // Update body class
    this.updateBodyClass();
  }

  private updateBodyClass(): void {
    if (this.isCollapsed) {
      document.body.classList.add('sidebar-collapsed');
    } else {
      document.body.classList.remove('sidebar-collapsed');
    }
  }

  private loadMenuPermissions(): void {
    this.subscription.add(this.userService.getCurrentUserMenuPermissions().subscribe({
      next: (menuKeys) => {
        const allowedKeys = new Set(menuKeys);
        this.visibleMenuItems = this.menuItems.filter(item => allowedKeys.has(item.key));
      },
      error: (error) => {
        console.error('Error loading menu permissions:', error);
        this.visibleMenuItems = [];
      }
    }));
  }
}
