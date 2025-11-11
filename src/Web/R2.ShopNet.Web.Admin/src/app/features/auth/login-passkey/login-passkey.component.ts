import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { PasskeyService } from '../../../core/services/passkey.service';
import { AuthPageLayoutComponent } from '../auth-page-layout/auth-page-layout.component';
import { ButtonComponent } from '../../../components/forms/button/button.component';
import { CheckboxComponent } from '../../../components/forms/checkbox/checkbox.component';
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
    CheckboxComponent,
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
      return;
    }

    if (!this.passkeyService.isWebAuthnSupported()) {
      this.errorMessage.set('Passkey authentication is not supported in this browser');
      return;
    }

    this.isPasskeyLoading.set(true);
    this.errorMessage.set(null);

    this.authService.loginWithPasskey(email).subscribe({
      next: () => {
        this.isPasskeyLoading.set(false);
        // Navigate to dashboard or return URL after successful login
        this.router.navigate([this.returnUrl]);
      },
      error: (error) => {
        this.isPasskeyLoading.set(false);

        // Handle different error scenarios
        if (error.name === 'NotAllowedError') {
          console.error('📛 [LoginComponent] User cancelled or timeout');
          this.errorMessage.set('Passkey authentication was cancelled or timed out');
        } else if (error.name === 'NotSupportedError') {
          console.error('📛 [LoginComponent] WebAuthn not supported');
          this.errorMessage.set('Passkey authentication is not supported');
        } else if (error.status === 404) {
          console.error('📛 [LoginComponent] No passkey found');
          this.errorMessage.set('No passkey found for this account');
        } else if (error.status === 400) {
          console.error('📛 [LoginComponent] Invalid passkey');
          this.errorMessage.set('Invalid passkey authentication');
        } else if (error.status === 0) {
          console.error('📛 [LoginComponent] Network/CORS error');
          this.errorMessage.set('Unable to connect to server. Please try again later.');
        } else {
          console.error('📛 [LoginComponent] Unknown error:', error);
          this.errorMessage.set('Passkey login failed. Please try again.');
        }
      }
    });
  }

  // Helper methods for template
  get emailControl() {
    return this.loginForm.get('email');
  }

}
