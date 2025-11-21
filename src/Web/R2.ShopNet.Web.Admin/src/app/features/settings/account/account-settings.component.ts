import { Component, OnInit, signal, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-account-settings',
  standalone: true,
  imports: [ ReactiveFormsModule],
  templateUrl: './account-settings.component.html'
})
export class AccountSettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);

  accountForm: FormGroup;
  saving = signal<boolean>(false);
  successMessage = signal<string>('');
  errorMessage = signal<string>('');

  user = this.auth.currentUser;

  constructor() {
    this.accountForm = this.fb.group({
      email: [{ value: '', disabled: true }],
      username: [{ value: '', disabled: true }],
      emailNotifications: [true],
      marketingEmails: [false]
    });
  }

  ngOnInit(): void {
    const currentUser = this.user();
    if (currentUser) {
      this.accountForm.patchValue({
        email: currentUser.email,
        username: currentUser.preferred_username || currentUser.email
      });
    }
  }

  onSubmit(): void {
    if (this.accountForm.invalid) return;

    this.saving.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    // Simulate save - replace with actual API call
    setTimeout(() => {
      this.saving.set(false);
      this.successMessage.set('Account settings updated successfully');

      // Clear success message after 3 seconds
      setTimeout(() => {
        this.successMessage.set('');
      }, 3000);
    }, 1000);
  }
}
