import { Component, ElementRef, ViewChild, PLATFORM_ID, inject, computed } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { SidebarService } from '../../core/services/sidebar.service';
import { ThemeService } from '../../core/services/theme.service';
import { AuthService } from '../../core/services/auth.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { IconComponent } from '../../shared/components/icon/icon.component';
import { UserDropdownComponent } from './user-dropdown/user-dropdown.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    IconComponent,
    UserDropdownComponent,
  ],
  templateUrl: './app-header.component.html',
})
export class AppHeaderComponent {
  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);
  private authService = inject(AuthService);
  
  isApplicationMenuOpen = false;
  readonly isMobileOpen$;
  readonly currentTheme$;
  
  // User data from auth service
  readonly user = computed(() => this.authService.currentUser());
  readonly userInitials = computed(() => {
    const user = this.user();
    if (!user) return 'A';
    
    // Try to get initials from name
    if (user.name) {
      const nameParts = user.name.split(' ');
      if (nameParts.length >= 2) {
        return (nameParts[0][0] + nameParts[nameParts.length - 1][0]).toUpperCase();
      }
      return user.name[0].toUpperCase();
    }
    
    // Fallback to email
    if (user.email) {
      return user.email[0].toUpperCase();
    }
    
    return 'A';
  });
  
  readonly displayName = computed(() => {
    const user = this.user();
    if (!user) return 'Admin';
    return user.name || user.email || 'Admin';
  });

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  constructor(
    public sidebarService: SidebarService,
    public themeService: ThemeService
  ) {
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
    this.currentTheme$ = this.themeService.theme$;
  }

  handleToggle() {
    if (this.isBrowser && window.innerWidth >= 1280) {
      this.sidebarService.toggleExpanded();
    } else {
      this.sidebarService.toggleMobileOpen();
    }
  }

  toggleApplicationMenu() {
    this.isApplicationMenuOpen = !this.isApplicationMenuOpen;
  }

  toggleTheme() {
    this.themeService.toggleTheme();
  }

  ngAfterViewInit() {
    if (this.isBrowser) {
      document.addEventListener('keydown', this.handleKeyDown);
    }
  }

  ngOnDestroy() {
    if (this.isBrowser) {
      document.removeEventListener('keydown', this.handleKeyDown);
    }
  }

  handleKeyDown = (event: KeyboardEvent) => {
    if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
      event.preventDefault();
      this.searchInput?.nativeElement.focus();
    }
  };
}
