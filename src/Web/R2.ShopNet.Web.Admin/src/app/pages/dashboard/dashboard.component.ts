import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [],
  template: `
    <div class="rounded-lg border border-gray-200 bg-white p-6 shadow-theme-sm dark:border-gray-800 dark:bg-gray-900">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-4">Dashboard</h1>
      <p class="text-gray-600 dark:text-gray-400">Welcome to R2.ShopNet Admin Portal!</p>
      
      <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div class="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-800 dark:bg-gray-800">
          <h3 class="text-sm font-medium text-gray-500 dark:text-gray-400">Total Users</h3>
          <p class="mt-2 text-3xl font-bold text-gray-900 dark:text-white">1,234</p>
        </div>
        <div class="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-800 dark:bg-gray-800">
          <h3 class="text-sm font-medium text-gray-500 dark:text-gray-400">Products</h3>
          <p class="mt-2 text-3xl font-bold text-gray-900 dark:text-white">567</p>
        </div>
        <div class="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-800 dark:bg-gray-800">
          <h3 class="text-sm font-medium text-gray-500 dark:text-gray-400">Orders</h3>
          <p class="mt-2 text-3xl font-bold text-gray-900 dark:text-white">89</p>
        </div>
        <div class="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-800 dark:bg-gray-800">
          <h3 class="text-sm font-medium text-gray-500 dark:text-gray-400">Revenue</h3>
          <p class="mt-2 text-3xl font-bold text-gray-900 dark:text-white">$12.3K</p>
        </div>
      </div>
    </div>
  `
})
export class DashboardComponent {}
