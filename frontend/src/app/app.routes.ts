import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ProjektiListComponent } from './components/projekti-list/projekti-list.component';
import { ProjekatDetailComponent } from './components/projekat-detail/projekat-detail.component';
import { KlijentiListComponent } from './components/klijenti-list/klijenti-list.component';
import { KlijentDetailComponent } from './components/klijent-detail/klijent-detail.component';
import { SettingsComponent } from './components/settings/settings.component';
import { AdminUsersComponent } from './components/admin-users/admin-users.component';
import { ImplementationModelsListComponent } from './components/implementation-models-list/implementation-models-list.component';
import { ImplementationModelEditComponent } from './components/implementation-model-edit/implementation-model-edit.component';
import { CalendarComponent } from './components/calendar/calendar.component';
import { AktivnostiListComponent } from './components/aktivnosti-list/aktivnosti-list.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { 
    path: 'dashboard', 
    component: DashboardComponent,
    canActivate: [AuthGuard]
  },
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
    path: 'kalendar', 
    component: CalendarComponent,
    canActivate: [AuthGuard]
  },
  { 
    path: 'aktivnosti', 
    component: AktivnostiListComponent,
    canActivate: [AuthGuard]
  },
  { 
    path: 'implementation-models', 
    component: ImplementationModelsListComponent,
    canActivate: [AuthGuard]
  },
  { 
    path: 'implementation-models/:id', 
    component: ImplementationModelEditComponent,
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
