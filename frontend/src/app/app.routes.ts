import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';
import { ProjektiListComponent } from './components/projekti-list/projekti-list.component';
import { ProjekatDetailComponent } from './components/projekat-detail/projekat-detail.component';
import { KlijentiListComponent } from './components/klijenti-list/klijenti-list.component';
import { KlijentDetailComponent } from './components/klijent-detail/klijent-detail.component';
import { SettingsComponent } from './components/settings/settings.component';
import { AdminUsersComponent } from './components/admin-users/admin-users.component';

export const routes: Routes = [
  { path: '', redirectTo: '/projekti', pathMatch: 'full' },
  { 
    path: 'projekti', 
    component: ProjektiListComponent,
    canActivate: [AuthGuard]
  },
  { 
    path: 'projekti/:id', 
    component: ProjekatDetailComponent,
    canActivate: [AuthGuard]
  },
  { 
    path: 'klijenti', 
    component: KlijentiListComponent,
    canActivate: [AuthGuard]
  },
  { 
    path: 'klijenti/:id', 
    component: KlijentDetailComponent,
    canActivate: [AuthGuard]
  },
  { 
    path: 'settings', 
    component: SettingsComponent,
    canActivate: [AuthGuard]
  },
  { 
    path: 'admin/users', 
    component: AdminUsersComponent,
    canActivate: [AuthGuard]
  }
];
