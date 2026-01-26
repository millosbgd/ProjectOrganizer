import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../services/user.service';
import { User } from '../../models/user.model';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css'
})
export class AdminUsersComponent implements OnInit {
  users: User[] = [];
  loading = false;
  currentUser: User | null = null;
  editingUser: User | null = null;

  constructor(private userService: UserService) {}

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadUsers();
  }

  loadCurrentUser(): void {
    this.userService.getCurrentUser().subscribe({
      next: (user) => {
        this.currentUser = user;
      },
      error: (error) => {
        console.error('Error loading current user:', error);
      }
    });
  }

  loadUsers(): void {
    this.loading = true;
    this.userService.getAllUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading users:', error);
        alert('Greška pri učitavanju korisnika. Proverite da li imate admin pristup.');
        this.loading = false;
      }
    });
  }

  editUser(user: User): void {
    this.editingUser = { ...user };
  }

  cancelEdit(): void {
    this.editingUser = null;
  }

  saveUser(): void {
    if (!this.editingUser) return;

    this.userService.updateUser(this.editingUser.id, this.editingUser).subscribe({
      next: () => {
        this.loadUsers();
        this.editingUser = null;
        alert('Korisnik je uspešno ažuriran!');
      },
      error: (error) => {
        console.error('Error updating user:', error);
        alert('Greška pri ažuriranju korisnika.');
      }
    });
  }

  deactivateUser(user: User): void {
    if (!confirm(`Da li ste sigurni da želite da deaktivirate korisnika ${user.email}?`)) {
      return;
    }

    this.userService.deactivateUser(user.id).subscribe({
      next: () => {
        this.loadUsers();
        alert('Korisnik je deaktiviran!');
      },
      error: (error) => {
        console.error('Error deactivating user:', error);
        alert('Greška pri deaktivaciji korisnika.');
      }
    });
  }

  isCurrentUser(user: User): boolean {
    return this.currentUser?.id === user.id;
  }

  get activeUsersCount(): number {
    return this.users.filter(u => u.isActive).length;
  }

  get adminUsersCount(): number {
    return this.users.filter(u => u.role === 'Admin').length;
  }
}
