import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { AuthPageLayoutComponent } from '../auth-page-layout/auth-page-layout.component';
import { ButtonComponent } from '../../../components/forms/button/button.component';
import { InputFieldComponent } from '../../../components/forms/input-field/input-field.component';
import { LabelComponent } from '../../../components/forms/label/label.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    LabelComponent,
    ButtonComponent,
    InputFieldComponent,
    AuthPageLayoutComponent
  ],
  templateUrl: './forgot-password.component.html'
})
export class ForgotPasswordComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  forgotPasswordForm!: FormGroup;
  isLoading = signal(false);
  isSuccess = signal(false);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmit(): void {
    if (this.forgotPasswordForm.invalid) {
      this.forgotPasswordForm.markAllAsTouched();
      this.errorMessage.set('Please enter a valid email address');
      return;
    }

    const { email } = this.forgotPasswordForm.value;

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.isSuccess.set(false);

    this.authService.forgotPassword(email).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        this.isSuccess.set(true);
      },
      error: (error) => {
        this.isLoading.set(false);

        if (error.status === 400) {
          this.errorMessage.set(error.error?.message || 'Invalid email address');
        } else if (error.status === 0) {
          this.errorMessage.set('Unable to connect to server. Please try again later.');
        } else {
          this.errorMessage.set('An error occurred. Please try again.');
        }
      }
    });
  }

  get emailControl() {
    return this.forgotPasswordForm.get('email');
  }
}
