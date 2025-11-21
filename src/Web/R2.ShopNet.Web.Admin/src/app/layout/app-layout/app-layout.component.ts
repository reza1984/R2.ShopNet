import { Component } from '@angular/core';
import { AsyncPipe, NgClass } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SidebarService } from '../../core/services/sidebar.service';
import { AppSidebarComponent } from '../app-sidebar/app-sidebar.component';
import { BackdropComponent } from '../backdrop/backdrop.component';
import { AppHeaderComponent } from '../app-header/app-header.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    AsyncPipe,
    NgClass,
    RouterOutlet,
    AppHeaderComponent,
    AppSidebarComponent,
    BackdropComponent
  ],
  templateUrl: './app-layout.component.html',
})
export class AppLayoutComponent {
  readonly isExpanded$;
  readonly isHovered$;
  readonly isMobileOpen$;

  constructor(public sidebarService: SidebarService) {
    this.isExpanded$ = this.sidebarService.isExpanded$;
    this.isHovered$ = this.sidebarService.isHovered$;
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
  }
}
