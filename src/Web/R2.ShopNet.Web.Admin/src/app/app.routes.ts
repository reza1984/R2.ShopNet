import { Routes } from '@angular/router';
import { AppLayoutComponent } from './layout/app-layout/app-layout.component';
import { UserListComponent } from './features/users/user-list/user-list.component';
import { UserEditComponent } from './features/users/user-edit/user-edit.component';
import { authGuard, publicGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Public routes (no authentication required)
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent),
    canActivate: [publicGuard]
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
    canActivate: [publicGuard]
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
    canActivate: [publicGuard]
  },

  // Protected routes (authentication required)
  {
    path: '',
    component: AppLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'users', component: UserListComponent },
      { path: 'users/:id/edit', component: UserEditComponent },
      { path: 'catalog/products', loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent) },
      { path: 'catalog/products/new', loadComponent: () => import('./features/products/product-form/product-form.component').then(m => m.ProductFormComponent) },
      { path: 'catalog/products/:id', loadComponent: () => import('./features/products/product-form/product-form.component').then(m => m.ProductFormComponent) },
      { path: 'catalog/categories', loadComponent: () => import('./features/categories/category-list/category-list.component').then(m => m.CategoryListComponent) },
      { path: 'catalog/categories/new', loadComponent: () => import('./features/categories/category-form/category-form.component').then(m => m.CategoryFormComponent) },
      { path: 'catalog/categories/:id', loadComponent: () => import('./features/categories/category-form/category-form.component').then(m => m.CategoryFormComponent) },
      { path: 'orders', loadComponent: () => import('./pages/orders/orders.component').then(m => m.OrdersComponent) },
      { path: 'reports', loadComponent: () => import('./pages/reports/reports.component').then(m => m.ReportsComponent) },
      { path: 'analytics', loadComponent: () => import('./pages/analytics/analytics.component').then(m => m.AnalyticsComponent) },
      {
        path: 'settings',
        loadComponent: () => import('./pages/settings/settings.component').then(m => m.SettingsComponent),
        children: [
          { path: '', redirectTo: 'account', pathMatch: 'full' },
          { path: 'account', loadComponent: () => import('./pages/settings/account/account-settings.component').then(m => m.AccountSettingsComponent) },
          { path: 'profile', loadComponent: () => import('./pages/settings/profile/profile-settings.component').then(m => m.ProfileSettingsComponent) },
          { path: 'security', loadComponent: () => import('./pages/settings/security/security-settings.component').then(m => m.SecuritySettingsComponent) },
          { path: 'support', loadComponent: () => import('./pages/settings/support/support.component').then(m => m.SupportComponent) }
        ]
      },
    ]
  },

  // Fallback route
  { path: '**', redirectTo: '/login' }
];
