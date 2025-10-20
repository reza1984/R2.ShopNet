import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { User } from '../../../core/models/user.model';
import { IconComponent } from '../../../shared/components/icon/icon.component';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    IconComponent
  ],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss']
})
export class UserListComponent implements OnInit {
  protected readonly Math = Math;
  
  searchTerm = '';
  filterStatus = signal<'all' | 'active' | 'inactive'>('all');
  selectedUsers = signal<Set<string>>(new Set());
  
  constructor(public userService: UserService) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.userService.getUsers(
      this.userService.currentPage(),
      this.userService.pageSize(),
      this.searchTerm
    ).subscribe({
      error: (error) => console.error('Error loading users:', error)
    });
  }

  onSearch(): void {
    this.userService.setPage(1);
    this.loadUsers();
  }

  setFilter(status: 'all' | 'active' | 'inactive'): void {
    this.filterStatus.set(status);
    this.loadUsers();
  }

  get filteredUsers(): User[] {
    const users = this.userService.users();
    const status = this.filterStatus();
    
    if (status === 'all') return users;
    return users.filter(u => status === 'active' ? u.isActive : !u.isActive);
  }

  toggleUserSelection(userId: string): void {
    const selected = new Set(this.selectedUsers());
    if (selected.has(userId)) {
      selected.delete(userId);
    } else {
      selected.add(userId);
    }
    this.selectedUsers.set(selected);
  }

  toggleAllUsers(): void {
    const users = this.filteredUsers;
    const selected = new Set(this.selectedUsers());
    
    if (selected.size === users.length) {
      this.selectedUsers.set(new Set());
    } else {
      this.selectedUsers.set(new Set(users.map(u => u.id)));
    }
  }

  isUserSelected(userId: string): boolean {
    return this.selectedUsers().has(userId);
  }

  get allUsersSelected(): boolean {
    const users = this.filteredUsers;
    return users.length > 0 && this.selectedUsers().size === users.length;
  }

  nextPage(): void {
    const totalPages = this.userService.totalPages();
    const currentPage = this.userService.currentPage();
    if (currentPage < totalPages) {
      this.userService.setPage(currentPage + 1);
      this.loadUsers();
    }
  }

  previousPage(): void {
    const currentPage = this.userService.currentPage();
    if (currentPage > 1) {
      this.userService.setPage(currentPage - 1);
      this.loadUsers();
    }
  }

  goToPage(page: number): void {
    this.userService.setPage(page);
    this.loadUsers();
  }

  get pageNumbers(): number[] {
    const total = this.userService.totalPages();
    const current = this.userService.currentPage();
    const delta = 2;
    const pages: number[] = [];
    
    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
      pages.push(i);
    }
    
    return pages;
  }

  toggleUserStatus(user: User): void {
    if (user.isActive) {
      this.userService.deactivateUser(user.id).subscribe({
        next: () => this.loadUsers(),
        error: (error) => console.error('Error deactivating user:', error)
      });
    } else {
      this.userService.activateUser(user.id).subscribe({
        next: () => this.loadUsers(),
        error: (error) => console.error('Error activating user:', error)
      });
    }
  }

  deleteUser(user: User): void {
    if (confirm(`Are you sure you want to delete ${user.fullName || user.email}?`)) {
      this.userService.deleteUser(user.id).subscribe({
        next: () => this.loadUsers(),
        error: (error) => console.error('Error deleting user:', error)
      });
    }
  }

  bulkDelete(): void {
    const count = this.selectedUsers().size;
    if (count === 0) return;
    
    if (confirm(`Are you sure you want to delete ${count} user(s)?`)) {
      // Implement bulk delete logic
      console.log('Bulk delete:', Array.from(this.selectedUsers()));
      this.selectedUsers.set(new Set());
    }
  }

  exportUsers(): void {
    console.log('Exporting users...');
    // Implement export logic
  }
}
