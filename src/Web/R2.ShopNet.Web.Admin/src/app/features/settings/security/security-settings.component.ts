import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';
import { PasskeyService } from '../../../core/services/passkey.service';
import { Passkey } from '../../../core/models/passkey.model';
import { environment } from '../../../../environments/environment.development';

@Component({
  selector: 'app-security-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './security-settings.component.html'
})
export class SecuritySettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private passkeyService = inject(PasskeyService);

  passwordForm: FormGroup;
  saving = signal<boolean>(false);
  successMessage = signal<string>('');
  errorMessage = signal<string>('');
  showCurrentPassword = signal<boolean>(false);
  showNewPassword = signal<boolean>(false);
  showConfirmPassword = signal<boolean>(false);

  // Passkey management
  passkeys = signal<Passkey[]>([]);
  loadingPasskeys = signal<boolean>(false);
  registeringPasskey = signal<boolean>(false);
  deletingPasskeyId = signal<string | null>(null);
  passkeySuccessMessage = signal<string>('');
  passkeyErrorMessage = signal<string>('');
  showPasskeyNameDialog = signal<boolean>(false);
  passkeyFriendlyName = signal<string>('');
  isWebAuthnSupported = signal<boolean>(false);
  isPlatformAuthAvailable = signal<boolean>(false);

  user = this.auth.currentUser;

  constructor() {
    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required, Validators.minLength(6)]],
      newPassword: ['', [Validators.required, Validators.minLength(8), this.passwordStrengthValidator]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  async ngOnInit(): Promise<void> {
    // Check WebAuthn support
    this.isWebAuthnSupported.set(this.passkeyService.isWebAuthnSupported());

    if (this.isWebAuthnSupported()) {
      this.isPlatformAuthAvailable.set(await this.passkeyService.isPlatformAuthenticatorAvailable());
      this.loadPasskeys();
    }
  }

  passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) return null;

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumeric = /[0-9]/.test(value);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(value);

    const passwordValid = hasUpperCase && hasLowerCase && hasNumeric && hasSpecialChar;

    return !passwordValid ? { passwordStrength: true } : null;
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    if (!newPassword || !confirmPassword) return null;

    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  }

  get passwordStrength(): { label: string; color: string; width: string } {
    const password = this.passwordForm.get('newPassword')?.value || '';

    if (!password) return { label: '', color: '', width: '0%' };

    let strength = 0;
    if (password.length >= 8) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) strength++;

    if (strength <= 2) return { label: 'Weak', color: 'bg-red-500', width: '33%' };
    if (strength <= 4) return { label: 'Medium', color: 'bg-yellow-500', width: '66%' };
    return { label: 'Strong', color: 'bg-green-500', width: '100%' };
  }

  onSubmit(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const currentUser = this.user();
    if (!currentUser?.email) {
      this.errorMessage.set('User email not found');
      return;
    }

    this.saving.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    const payload = {
      email: currentUser.email,
      currentPassword: this.passwordForm.value.currentPassword,
      newPassword: this.passwordForm.value.newPassword,
      confirmPassword: this.passwordForm.value.confirmPassword
    };

    this.http.post(`${environment.apiUrl}/api/auth/change-password`, payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set('Password changed successfully');
        this.passwordForm.reset();

        // Clear success message after 5 seconds
        setTimeout(() => {
          this.successMessage.set('');
        }, 5000);
      },
      error: (error) => {
        console.error('Error changing password:', error);
        this.saving.set(false);

        // Extract error message from response
        const errorMsg = error.error?.message || error.error?.title || 'Failed to change password';
        this.errorMessage.set(errorMsg);
      }
    });
  }

  togglePasswordVisibility(field: 'current' | 'new' | 'confirm'): void {
    switch (field) {
      case 'current':
        this.showCurrentPassword.update(v => !v);
        break;
      case 'new':
        this.showNewPassword.update(v => !v);
        break;
      case 'confirm':
        this.showConfirmPassword.update(v => !v);
        break;
    }
  }

  // Passkey Management Methods

  loadPasskeys(): void {
    this.loadingPasskeys.set(true);
    this.passkeyService.getUserPasskeys().subscribe({
      next: (passkeys) => {
        this.passkeys.set(passkeys);
        this.loadingPasskeys.set(false);
      },
      error: (error) => {
        console.error('Error loading passkeys:', error);
        this.loadingPasskeys.set(false);
        // Don't show error if endpoint doesn't exist yet
        if (error.status !== 404) {
          this.passkeyErrorMessage.set('Failed to load passkeys');
        }
      }
    });
  }

  openPasskeyNameDialog(): void {
    this.showPasskeyNameDialog.set(true);
    this.passkeyFriendlyName.set('');
  }

  closePasskeyNameDialog(): void {
    this.showPasskeyNameDialog.set(false);
    this.passkeyFriendlyName.set('');
  }

  registerPasskey(): void {
    const friendlyName = this.passkeyFriendlyName().trim() || this.getDefaultPasskeyName();

    this.registeringPasskey.set(true);
    this.passkeySuccessMessage.set('');
    this.passkeyErrorMessage.set('');
    this.closePasskeyNameDialog();

    this.passkeyService.registerPasskey(friendlyName).subscribe({
      next: (response) => {
        this.registeringPasskey.set(false);
        this.passkeySuccessMessage.set('Passkey registered successfully');
        this.loadPasskeys();

        setTimeout(() => {
          this.passkeySuccessMessage.set('');
        }, 5000);
      },
      error: (error) => {
        console.error('Error registering passkey:', error);
        this.registeringPasskey.set(false);

        let errorMsg = 'Failed to register passkey';
        if (error.name === 'NotAllowedError') {
          errorMsg = 'Passkey registration was cancelled or not allowed';
        } else if (error.error?.message) {
          errorMsg = error.error.message;
        }

        this.passkeyErrorMessage.set(errorMsg);
      }
    });
  }

  deletePasskey(passkey: Passkey): void {
    if (!confirm(`Are you sure you want to remove "${passkey.deviceName}"?`)) {
      return;
    }

    this.deletingPasskeyId.set(passkey.id);
    this.passkeyErrorMessage.set('');

    this.passkeyService.deletePasskey(passkey.id).subscribe({
      next: () => {
        this.deletingPasskeyId.set(null);
        this.passkeySuccessMessage.set('Passkey removed successfully');
        this.loadPasskeys();

        setTimeout(() => {
          this.passkeySuccessMessage.set('');
        }, 3000);
      },
      error: (error) => {
        console.error('Error deleting passkey:', error);
        this.deletingPasskeyId.set(null);
        this.passkeyErrorMessage.set('Failed to remove passkey');
      }
    });
  }

  getDefaultPasskeyName(): string {
    const ua = navigator.userAgent;
    if (ua.includes('Mac')) return 'Mac Touch ID';
    if (ua.includes('iPhone')) return 'iPhone';
    if (ua.includes('iPad')) return 'iPad';
    if (ua.includes('Android')) return 'Android Device';
    if (ua.includes('Windows')) return 'Windows Hello';
    return 'Security Key';
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }


}
