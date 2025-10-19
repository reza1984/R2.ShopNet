import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { User } from '../../../core/models/user.model';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatPaginatorModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss']
})
export class UserListComponent implements OnInit {
  displayedColumns: string[] = ['email', 'fullName', 'roles', 'isActive', 'lastLoginAt', 'actions'];
  searchTerm = signal<string>('');
  
  constructor(public userService: UserService) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.userService.getUsers(
      this.userService.currentPage(),
      this.userService.pageSize(),
      this.searchTerm()
    ).subscribe({
      error: (error) => console.error('Error loading users:', error)
    });
  }

  onSearch(): void {
    this.userService.setPage(1);
    this.loadUsers();
  }

  onPageChange(event: PageEvent): void {
    this.userService.setPage(event.pageIndex + 1);
    this.loadUsers();
  }

  toggleUserStatus(user: User): void {
    if (user.isActive) {
      this.userService.deactivateUser(user.id).subscribe({
        next: () => {
          console.log('User deactivated successfully');
          this.loadUsers();
        },
        error: (error) => console.error('Error deactivating user:', error)
      });
    } else {
      this.userService.activateUser(user.id).subscribe({
        next: () => {
          console.log('User activated successfully');
          this.loadUsers();
        },
        error: (error) => console.error('Error activating user:', error)
      });
    }
  }

  deleteUser(user: User): void {
    if (confirm(`Are you sure you want to delete ${user.fullName || user.email}?`)) {
      this.userService.deleteUser(user.id).subscribe({
        next: () => {
          console.log('User deleted successfully');
          this.loadUsers();
        },
        error: (error) => console.error('Error deleting user:', error)
      });
    }
  }
}
