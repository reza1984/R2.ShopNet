import { Routes } from '@angular/router';
import { UserListComponent } from './features/users/user-list/user-list.component';
import { UserEditComponent } from './features/users/user-edit/user-edit.component';

export const routes: Routes = [
  { path: '', redirectTo: '/users', pathMatch: 'full' },
  { path: 'users', component: UserListComponent },
  { path: 'users/:id/edit', component: UserEditComponent },
  { path: '**', redirectTo: '/users' }
];
