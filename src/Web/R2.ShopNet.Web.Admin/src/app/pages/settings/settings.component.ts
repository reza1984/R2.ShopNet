import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterLink, RouterLinkActive],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Settings</h1>
        <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Manage your account settings and preferences</p>
      </div>

      <!-- Settings Layout -->
      <div class="grid gap-6 lg:grid-cols-[260px_1fr]">
        <!-- Settings Navigation -->
        <nav class="space-y-1">
          <a
            routerLink="/settings/account"
            routerLinkActive="bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-400"
            [routerLinkActiveOptions]="{exact: false}"
            class="flex items-center gap-3 rounded-lg px-4 py-3 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            <span class="material-symbols-rounded text-xl">account_circle</span>
            <span>Account</span>
          </a>
          <a
            routerLink="/settings/profile"
            routerLinkActive="bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-400"
            [routerLinkActiveOptions]="{exact: false}"
            class="flex items-center gap-3 rounded-lg px-4 py-3 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            <span class="material-symbols-rounded text-xl">person</span>
            <span>Profile</span>
          </a>
          <a
            routerLink="/settings/security"
            routerLinkActive="bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-400"
            [routerLinkActiveOptions]="{exact: false}"
            class="flex items-center gap-3 rounded-lg px-4 py-3 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            <span class="material-symbols-rounded text-xl">security</span>
            <span>Security</span>
          </a>
          <a
            routerLink="/settings/support"
            routerLinkActive="bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-400"
            [routerLinkActiveOptions]="{exact: false}"
            class="flex items-center gap-3 rounded-lg px-4 py-3 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            <span class="material-symbols-rounded text-xl">help</span>
            <span>Support</span>
          </a>
        </nav>

        <!-- Settings Content -->
        <div>
          <router-outlet></router-outlet>
        </div>
      </div>
    </div>
  `
})
export class SettingsComponent {}
