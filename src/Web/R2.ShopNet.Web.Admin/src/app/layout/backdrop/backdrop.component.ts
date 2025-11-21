import { Component } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { SidebarService } from '../../core/services/sidebar.service';

@Component({
  selector: 'app-backdrop',
  standalone: true,
  imports: [AsyncPipe],
  templateUrl: './backdrop.component.html',
})
export class BackdropComponent {
  readonly isMobileOpen$;

  constructor(private sidebarService: SidebarService) {
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
  }

  closeSidebar() {
    this.sidebarService.setMobileOpen(false);
  }
}
