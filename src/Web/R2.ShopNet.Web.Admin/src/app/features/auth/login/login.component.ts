import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
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
  templateUrl: './login.component.html'
})
export class LoginComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  loginForm!: FormGroup;
  showPassword = false;
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  private returnUrl: string = '/dashboard';

  ngOnInit(): void {
    console.log('🎬 [LoginComponent] Initializing...');

    // Initialize reactive form
    this.loginForm = this.fb.group({
      email: ['admin@shopnet.com', [Validators.required, Validators.email]],
      password: ['Admin@123', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false]
    });

    // Get the returnUrl from query params, default to /dashboard
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
    console.log('🎯 [LoginComponent] Return URL:', this.returnUrl);
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSignIn(): void {
    console.log('🔵 [LoginComponent] Form submitted');
    console.log('📋 [LoginComponent] Form valid:', this.loginForm.valid);
    console.log('📋 [LoginComponent] Form value:', this.loginForm.value);

    // Mark all fields as touched to show validation errors
    if (this.loginForm.invalid) {
      console.warn('⚠️  [LoginComponent] Form is invalid');
      this.loginForm.markAllAsTouched();
      this.errorMessage.set('Please enter valid email and password');
      return;
    }

    const { email, password } = this.loginForm.value;
    console.log('📧 [LoginComponent] Email:', email);
    console.log('🔑 [LoginComponent] Password length:', password?.length || 0);

    console.log('⏳ [LoginComponent] Setting loading state...');
    this.isLoading.set(true);
    this.errorMessage.set(null);

    console.log('🚀 [LoginComponent] Calling AuthService.login()...');
    this.authService.login(email, password).subscribe({
      next: () => {
        console.log('✅ [LoginComponent] Login successful!');
        console.log('🔐 [LoginComponent] Is authenticated:', this.authService.isAuthenticated());
        console.log('👤 [LoginComponent] Current user:', this.authService.currentUser());
        console.log('🎯 [LoginComponent] Return URL:', this.returnUrl);

        this.isLoading.set(false);
        console.log('🧭 [LoginComponent] Navigating to:', this.returnUrl);

        // Navigate to the original requested URL or dashboard
        this.router.navigateByUrl(this.returnUrl).then(success => {
          console.log('✅ [LoginComponent] Navigation result:', success ? 'SUCCESS' : 'FAILED');
        });
      },
      error: (error) => {
        console.error('❌ [LoginComponent] Login error received');
        console.error('📛 [LoginComponent] Error object:', error);

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

  // Helper methods for template
  get emailControl() {
    return this.loginForm.get('email');
  }

  get passwordControl() {
    return this.loginForm.get('password');
  }

  get rememberMeControl() {
    return this.loginForm.get('rememberMe');
  }
}
