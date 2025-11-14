import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { PasskeyService } from '../../../core/services/passkey.service';
import { AuthPageLayoutComponent } from '../auth-page-layout/auth-page-layout.component';
import { ButtonComponent } from '../../../components/forms/button/button.component';
import { InputFieldComponent } from '../../../components/forms/input-field/input-field.component';
import { LabelComponent } from '../../../components/forms/label/label.component';
import { IconComponent } from '../../../components/icon/icon.component';

@Component({
  selector: 'app-login',
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
  templateUrl: './login-passkey.component.html'
})
export class LoginWithPasskeyComponent implements OnInit {
  private authService = inject(AuthService);
  private passkeyService = inject(PasskeyService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  loginForm!: FormGroup;
  showPassword = false;
  isLoading = signal(false);
  isPasskeyLoading = signal(false);
  errorMessage = signal<string | null>(null);
  isPasskeySupported = signal(false);
  private returnUrl: string = '/dashboard';

  ngOnInit(): void {

    // Initialize reactive form
    this.loginForm = this.fb.group({
      email: ['admin@shopnet.com', [Validators.required, Validators.email]],
    });

    // Get the returnUrl from query params, default to /dashboard
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';

    // Check if passkey is supported
    this.isPasskeySupported.set(this.passkeyService.isWebAuthnSupported());
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSignIn(): void {
    // Mark all fields as touched to show validation errors
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      this.errorMessage.set('Please enter valid email and password');
      return;
    }

    const { email, password } = this.loginForm.value;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login(email, password).subscribe({
      next: () => {
        this.isLoading.set(false);

      },
      error: (error) => {
      
        this.isLoading.set(false);

        // Handle different error scenarios
        if (error.status === 400) {
          console.error('📛 [LoginComponent] 400 Bad Request - Invalid credentials');
          this.errorMessage.set('Invalid email or password');
        } else if (error.status === 0) {
          console.error('📛 [LoginComponent] Status 0 - Network/CORS error');
          this.errorMessage.set('Unable to connect to server. Please try again later.');
        } else {
          console.error('📛 [LoginComponent] Status', error.status, '- Unknown error');
          this.errorMessage.set('Login failed. Please try again.');
        }
      }
    });
  }

  onSignInWithPasskey(): void {
    const email = this.loginForm.get('email')?.value;

    if (!email) {
      this.errorMessage.set('Please enter your email address');
      this.loginForm.get('email')?.markAsTouched();
      return;
    }

    if (!this.passkeyService.isWebAuthnSupported()) {
      this.errorMessage.set('Passkey authentication is not supported on this browser or device. Please use a different sign-in method.');
      return;
    }

    this.isPasskeyLoading.set(true);
    this.errorMessage.set(null);

    this.authService.loginWithPasskey(email).subscribe({
      next: () => {
        this.isPasskeyLoading.set(false);
        console.log('✅ Passkey authentication successful');
        // Navigate to dashboard or return URL after successful login
        this.router.navigate([this.returnUrl]);
      },
      error: (error) => {
        this.isPasskeyLoading.set(false);

        // Use the centralized error handling from PasskeyService
        const errorMessage = this.passkeyService.getErrorMessage(error);
        this.errorMessage.set(errorMessage);

        console.error('❌ Passkey authentication failed:', error);
      }
    });
  }

  // Helper methods for template
  get emailControl() {
    return this.loginForm.get('email');
  }

}
