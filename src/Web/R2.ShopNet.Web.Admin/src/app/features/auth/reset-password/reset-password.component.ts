import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { AuthPageLayoutComponent } from '../auth-page-layout/auth-page-layout.component';
import { ButtonComponent } from '../../../components/forms/button/button.component';
import { InputFieldComponent } from '../../../components/forms/input-field/input-field.component';
import { LabelComponent } from '../../../components/forms/label/label.component';
import { IconComponent } from '../../../components/icon/icon.component';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    LabelComponent,
    ButtonComponent,
    InputFieldComponent,
    IconComponent,
    AuthPageLayoutComponent
  ],
  templateUrl: './reset-password.component.html'
})
export class ResetPasswordComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  resetPasswordForm!: FormGroup;
  showPassword = false;
  showConfirmPassword = false;
  isLoading = signal(false);
  isSuccess = signal(false);
  errorMessage = signal<string | null>(null);

  private token: string = '';
  private email: string = '';

  ngOnInit(): void {
    // Get token and email from query parameters
    this.route.queryParams.subscribe(params => {
      this.token = params['token'] || '';
      this.email = params['email'] || '';

      if (!this.token || !this.email) {
        this.errorMessage.set('Invalid password reset link. Please request a new one.');
      }
    });

    this.resetPasswordForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('newPassword');
    const confirmPassword = form.get('confirmPassword');

    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }

    return null;
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  onSubmit(): void {
    if (this.resetPasswordForm.invalid) {
      this.resetPasswordForm.markAllAsTouched();
      this.errorMessage.set('Please fix the errors below');
      return;
    }

    if (!this.token || !this.email) {
      this.errorMessage.set('Invalid password reset link. Please request a new one.');
      return;
    }

    const { newPassword, confirmPassword } = this.resetPasswordForm.value;

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.isSuccess.set(false);

    this.authService.resetPassword(this.email, this.token, newPassword, confirmPassword).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        this.isSuccess.set(true);

        // Redirect to login after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (error) => {
        this.isLoading.set(false);

        if (error.status === 400) {
          const message = error.error?.message || 'Failed to reset password';
          this.errorMessage.set(message);
        } else if (error.status === 0) {
          this.errorMessage.set('Unable to connect to server. Please try again later.');
        } else {
          this.errorMessage.set('An error occurred. Please try again.');
        }
      }
    });
  }

  get newPasswordControl() {
    return this.resetPasswordForm.get('newPassword');
  }

  get confirmPasswordControl() {
    return this.resetPasswordForm.get('confirmPassword');
  }
}
