import { Component, OnInit, signal, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { UserService } from '../../../core/services/user.service';

@Component({
  selector: 'app-profile-settings',
  standalone: true,
  imports: [ ReactiveFormsModule],
  templateUrl: './profile-settings.component.html'
})
export class ProfileSettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private userService = inject(UserService);

  profileForm: FormGroup;
  saving = signal<boolean>(false);
  loading = signal<boolean>(false);
  successMessage = signal<string>('');
  errorMessage = signal<string>('');

  user = this.auth.currentUser;

  constructor() {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      phoneNumber: ['']
    });
  }

  ngOnInit(): void {
    const currentUser = this.user();
    if (currentUser) {
      this.loadUserProfile();
    }
  }

  loadUserProfile(): void {
    const currentUser = this.user();
    if (!currentUser?.sub) return;

    this.loading.set(true);
    this.userService.getUserById(currentUser.sub).subscribe({
      next: (user) => {
        this.profileForm.patchValue({
          firstName: user.firstName || '',
          lastName: user.lastName || '',
          phoneNumber: user.phoneNumber || ''
        });
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading user profile:', error);
        this.errorMessage.set('Failed to load profile data');
        this.loading.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.profileForm.invalid) return;

    const currentUser = this.user();
    if (!currentUser?.sub) return;

    this.saving.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    this.userService.updateUser(currentUser.sub, this.profileForm.value).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set('Profile updated successfully');

        // Clear success message after 3 seconds
        setTimeout(() => {
          this.successMessage.set('');
        }, 3000);
      },
      error: (error) => {
        console.error('Error updating profile:', error);
        this.saving.set(false);
        this.errorMessage.set('Failed to update profile');
      }
    });
  }
}
