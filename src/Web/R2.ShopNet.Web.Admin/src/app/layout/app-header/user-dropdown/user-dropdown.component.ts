import { Component, inject, computed } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { IconComponent } from '../../../components/icon/icon.component';
import { DropdownComponent } from '../../../components/ui/dropdown/dropdown.component';


@Component({
  selector: 'app-user-dropdown',
  templateUrl: './user-dropdown.component.html',
  imports:[CommonModule,RouterModule,DropdownComponent,IconComponent]
})
export class UserDropdownComponent {
  isOpen = false;
  private auth = inject(AuthService);
  readonly user = computed(() => this.auth.currentUser());

  signOut() {
    this.auth.logout();
  }

  toggleDropdown() {
    this.isOpen = !this.isOpen;
  }

  closeDropdown() {
    this.isOpen = false;
  }

  get displayName(): string {
    const user = this.user();
    if (!user) return 'User';
    return user.name || user.preferred_username || user.email || 'User';
  }

  get email(): string {
    const user = this.user();
    return user?.email || '';
  }

  get avatarUrl(): string {
    // If you have user avatar logic, use it here. Fallback to default image.
    return '/images/user/owner.png';
  }
}