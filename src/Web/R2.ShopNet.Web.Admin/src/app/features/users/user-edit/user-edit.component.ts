import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { User } from '../../../core/models/user.model';

@Component({
  selector: 'app-user-edit',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule
  ],
  templateUrl: './user-edit.component.html',
  styleUrls: ['./user-edit.component.scss']
})
export class UserEditComponent implements OnInit {
  userForm: FormGroup;
  user = signal<User | null>(null);
  loading = signal<boolean>(false);
  saving = signal<boolean>(false);
  userId: string | null = null;

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.userForm = this.fb.group({
      firstName: [''],
      lastName: [''],
      phoneNumber: ['']
    });
  }

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id');
    if (this.userId) {
      this.loadUser();
    }
  }

  loadUser(): void {
    if (!this.userId) return;
    
    this.loading.set(true);
    this.userService.getUserById(this.userId).subscribe({
      next: (user) => {
        this.user.set(user);
        this.userForm.patchValue({
          firstName: user.firstName,
          lastName: user.lastName,
          phoneNumber: user.phoneNumber
        });
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading user:', error);
        this.loading.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.userForm.invalid || !this.userId) return;

    this.saving.set(true);
    this.userService.updateUser(this.userId, this.userForm.value).subscribe({
      next: () => {
        console.log('User updated successfully');
        this.saving.set(false);
        this.router.navigate(['/users']);
      },
      error: (error) => {
        console.error('Error updating user:', error);
        this.saving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/users']);
  }
}
