import { Component } from '@angular/core';
@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [],
  template: `
    <div class="rounded-lg border border-gray-200 bg-white p-6 shadow-theme-sm dark:border-gray-800 dark:bg-gray-900">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-4">Orders Management</h1>
      <p class="text-gray-600 dark:text-gray-400">View and manage customer orders.</p>
    </div>
  `
})
export class OrdersComponent {}
