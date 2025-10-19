# TailAdmin Angular - Implementation Guide & Code Examples

## Component Implementation Patterns

### 1. Basic UI Component Structure

#### Button Component Example
```typescript
import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { SafeHtmlPipe } from '../../../pipe/safe-html.pipe';

@Component({
  selector: 'app-button',
  imports: [CommonModule, SafeHtmlPipe],
  templateUrl: './button.component.html',
  styles: ``,
})
export class ButtonComponent {
  @Input() size: 'sm' | 'md' = 'md';
  @Input() variant: 'primary' | 'outline' = 'primary';
  @Input() disabled = false;
  @Input() className = '';
  @Input() startIcon?: string;
  @Input() endIcon?: string;
  @Output() btnClick = new EventEmitter<Event>();

  get sizeClasses(): string {
    return this.size === 'sm'
      ? 'px-4 py-3 text-sm'
      : 'px-5 py-3.5 text-sm';
  }

  get variantClasses(): string {
    return this.variant === 'primary'
      ? 'bg-brand-500 text-white shadow-theme-xs hover:bg-brand-600 disabled:bg-brand-300'
      : 'bg-white text-gray-700 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 dark:bg-gray-800 dark:text-gray-400 dark:ring-gray-700 dark:hover:bg-white/[0.03] dark:hover:text-gray-300';
  }

  onClick(event: Event) {
    if (!this.disabled) {
      this.btnClick.emit(event);
    }
  }
}
```

#### Button Template
```html
<button
  type="button"
  [ngClass]="
    'inline-flex items-center justify-center gap-2 rounded-lg transition ' +
    className + ' ' +
    sizeClasses + ' ' +
    variantClasses + ' ' +
    (disabled ? 'cursor-not-allowed opacity-50' : '')
  "
  [disabled]="disabled"
  (click)="onClick($event)"
>
  @if (startIcon) {
    <span class="flex items-center" [innerHTML]="startIcon | safeHtml"></span>
  }
  <ng-content></ng-content>
  @if (endIcon) {
    <span class="flex items-center" [innerHTML]="endIcon | safeHtml"></span>
  }
</button>
```

### 2. Form Component Pattern

#### Input Field Component
```typescript
import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-input-field',
  imports: [CommonModule],
  template: `
    <div class="relative">
      <input
        [type]="type"
        [id]="id"
        [name]="name"
        [placeholder]="placeholder"
        [value]="value"
        [disabled]="disabled"
        [ngClass]="inputClasses"
        (input)="onInput($event)"
      />
      @if (hint) {
        <p class="mt-1.5 text-xs"
          [ngClass]="{
            'text-error-500': error,
            'text-success-500': success,
            'text-gray-500': !error && !success
          }">
          {{ hint }}
        </p>
      }
    </div>
  `,
})
export class InputFieldComponent {
  @Input() type: string = 'text';
  @Input() placeholder?: string = '';
  @Input() value: string | number = '';
  @Input() disabled: boolean = false;
  @Input() error: boolean = false;
  @Input() success: boolean = false;
  @Input() hint?: string;
  @Input() className: string = '';

  @Output() valueChange = new EventEmitter<string | number>();

  get inputClasses(): string {
    let classes = `h-11 w-full rounded-lg border appearance-none px-4 py-2.5 text-sm shadow-theme-xs 
                   placeholder:text-gray-400 focus:outline-hidden focus:ring-3 dark:bg-gray-900 
                   dark:text-white/90 dark:placeholder:text-white/30 ${this.className}`;

    if (this.disabled) {
      classes += ` text-gray-500 border-gray-300 opacity-40 bg-gray-100 cursor-not-allowed 
                   dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700`;
    } else if (this.error) {
      classes += ` border-error-500 focus:border-error-300 focus:ring-error-500/20 
                   dark:text-error-400 dark:border-error-500 dark:focus:border-error-800`;
    } else if (this.success) {
      classes += ` border-success-500 focus:border-success-300 focus:ring-success-500/20 
                   dark:text-success-400 dark:border-success-500 dark:focus:border-success-800`;
    } else {
      classes += ` bg-transparent text-gray-800 border-gray-300 focus:border-brand-300 
                   focus:ring-brand-500/20 dark:border-gray-700 dark:text-white/90 
                   dark:focus:border-brand-800`;
    }
    return classes;
  }

  onInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.valueChange.emit(this.type === 'number' ? +input.value : input.value);
  }
}
```

### 3. Card Component Pattern

#### Card Base Components (Need to be created)
```typescript
// card.component.ts
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-card',
  imports: [CommonModule],
  template: `<div [ngClass]="'rounded-lg border border-gray-200 bg-white shadow-theme-sm dark:border-gray-800 dark:bg-gray-900 ' + className">
    <ng-content></ng-content>
  </div>`,
})
export class CardComponent {
  @Input() className = '';
}

// card-title.component.ts
@Component({
  selector: 'app-card-title',
  imports: [CommonModule],
  template: `<h3 [ngClass]="'text-lg font-semibold text-gray-900 dark:text-white ' + className">
    <ng-content></ng-content>
  </h3>`,
})
export class CardTitleComponent {
  @Input() className = '';
}

// card-description.component.ts
@Component({
  selector: 'app-card-description',
  imports: [CommonModule],
  template: `<p [ngClass]="'text-sm text-gray-500 dark:text-gray-400 ' + className">
    <ng-content></ng-content>
  </p>`,
})
export class CardDescriptionComponent {
  @Input() className = '';
}
```

#### Using Cards
```html
<app-card>
  <div class="p-5">
    <div class="mb-5 overflow-hidden rounded-lg">
      <img src="/images/cards/card-01.png" alt="card" class="rounded-lg" />
    </div>
    <div>
      <app-card-title>Card title</app-card-title>
      <app-card-description>
        Lorem ipsum dolor sit amet, consectetur adipisicing elit.
      </app-card-description>
      <button class="inline-flex items-center gap-2 px-4 py-3 mt-4 text-sm font-medium 
                      text-white rounded-lg bg-brand-500 shadow-theme-xs hover:bg-brand-600">
        Read more
      </button>
    </div>
  </div>
</app-card>
```

### 4. Composable Table Pattern

#### Table Composition
```html
<app-table>
  <app-table-header>
    <app-table-row>
      <app-table-cell class="font-semibold">Name</app-table-cell>
      <app-table-cell class="font-semibold">Email</app-table-cell>
      <app-table-cell class="font-semibold">Status</app-table-cell>
    </app-table-row>
  </app-table-header>
  <app-table-body>
    @for (item of items; track item.id) {
      <app-table-row>
        <app-table-cell>{{ item.name }}</app-table-cell>
        <app-table-cell>{{ item.email }}</app-table-cell>
        <app-table-cell>
          <app-badge [color]="item.status === 'active' ? 'success' : 'error'">
            {{ item.status }}
          </app-badge>
        </app-table-cell>
      </app-table-row>
    }
  </app-table-body>
</app-table>
```

### 5. Form Control Pattern with Validation

#### Reactive Form Example
```typescript
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { InputFieldComponent } from '../form/input/input-field.component';
import { CheckboxComponent } from '../form/input/checkbox.component';
import { ButtonComponent } from '../ui/button/button.component';

@Component({
  selector: 'app-user-form',
  imports: [ReactiveFormsModule, InputFieldComponent, CheckboxComponent, ButtonComponent],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()">
      <div class="space-y-4">
        <app-input-field
          type="text"
          placeholder="Enter name"
          [value]="form.get('name')?.value"
          (valueChange)="form.patchValue({ name: $event })"
          [error]="form.get('name')?.invalid && form.get('name')?.touched"
          hint="Enter your full name"
        />
        
        <app-input-field
          type="email"
          placeholder="Enter email"
          [value]="form.get('email')?.value"
          (valueChange)="form.patchValue({ email: $event })"
          [error]="form.get('email')?.invalid && form.get('email')?.touched"
          hint="We'll never share your email"
        />
        
        <app-checkbox
          label="Subscribe to newsletter"
          [checked]="form.get('subscribe')?.value"
          (checkedChange)="form.patchValue({ subscribe: $event })"
        />
        
        <app-button
          (btnClick)="onSubmit()"
          [disabled]="form.invalid"
        >
          Submit
        </app-button>
      </div>
    </form>
  `,
})
export class UserFormComponent implements OnInit {
  form!: FormGroup;

  constructor(private fb: FormBuilder) {}

  ngOnInit() {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      subscribe: [false],
    });
  }

  onSubmit() {
    if (this.form.valid) {
      console.log(this.form.value);
    }
  }
}
```

### 6. Layout Integration Example

#### Page Layout
```typescript
import { Component } from '@angular/core';
import { AppLayoutComponent } from '../layout/app-layout/app-layout.component';

@Component({
  selector: 'app-dashboard',
  imports: [AppLayoutComponent],
  template: `
    <app-layout>
      <div class="p-6">
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Dashboard</h1>
        
        <!-- Main content goes here -->
        <div class="grid grid-cols-12 gap-6 mt-6">
          <!-- Metrics Row -->
          <div class="col-span-12 md:col-span-3">
            <app-card>
              <div class="p-5">
                <div class="text-sm font-medium text-gray-500 dark:text-gray-400">Total Users</div>
                <div class="text-3xl font-bold text-gray-900 dark:text-white mt-2">1,234</div>
              </div>
            </app-card>
          </div>
          
          <!-- Charts Row -->
          <div class="col-span-12 md:col-span-6">
            <app-card>
              <div class="p-5">
                <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Sales Chart</h3>
                <app-line-chart-one />
              </div>
            </app-card>
          </div>
        </div>
      </div>
    </app-layout>
  `,
})
export class DashboardComponent {}
```

### 7. Modal Component Usage

#### Modal Implementation
```typescript
@Component({
  selector: 'app-modal-example',
  template: `
    <button 
      (click)="isOpen = true"
      class="px-4 py-2 bg-brand-500 text-white rounded-lg hover:bg-brand-600"
    >
      Open Modal
    </button>

    @if (isOpen) {
      <div class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
        <app-modal (click)="isOpen = false">
          <div class="bg-white rounded-lg dark:bg-gray-900 p-6 max-w-md w-full">
            <h2 class="text-xl font-bold mb-4 text-gray-900 dark:text-white">Modal Title</h2>
            <p class="text-gray-600 dark:text-gray-400 mb-6">Modal content goes here</p>
            <div class="flex gap-3 justify-end">
              <button 
                (click)="isOpen = false"
                class="px-4 py-2 border rounded-lg hover:bg-gray-50 dark:border-gray-700 dark:hover:bg-gray-800"
              >
                Cancel
              </button>
              <app-button (btnClick)="confirmAction()">
                Confirm
              </app-button>
            </div>
          </div>
        </app-modal>
      </div>
    }
  `,
})
export class ModalExampleComponent {
  isOpen = false;

  confirmAction() {
    console.log('Confirmed');
    this.isOpen = false;
  }
}
```

### 8. Dark Mode Implementation

#### Theme Toggle
```typescript
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-theme-toggle',
  template: `
    <button 
      (click)="toggleTheme()"
      [attr.aria-label]="isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'"
      class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800"
    >
      @if (isDarkMode) {
        <svg class="w-5 h-5 text-yellow-400"><!-- Sun icon --></svg>
      } @else {
        <svg class="w-5 h-5 text-gray-600"><!-- Moon icon --></svg>
      }
    </button>
  `,
})
export class ThemeToggleComponent implements OnInit {
  isDarkMode = false;

  ngOnInit() {
    // Check if dark mode is enabled in localStorage or system preference
    const isDark = localStorage.getItem('theme') === 'dark' ||
                   (!localStorage.getItem('theme') && 
                    window.matchMedia('(prefers-color-scheme: dark)').matches);
    this.isDarkMode = isDark;
    this.applyTheme();
  }

  toggleTheme() {
    this.isDarkMode = !this.isDarkMode;
    this.applyTheme();
  }

  private applyTheme() {
    if (this.isDarkMode) {
      document.documentElement.classList.add('dark');
      localStorage.setItem('theme', 'dark');
    } else {
      document.documentElement.classList.remove('dark');
      localStorage.setItem('theme', 'light');
    }
  }
}
```

### 9. Dropdown Component Pattern

#### Dropdown Usage
```html
<div class="relative">
  <button 
    class="px-4 py-2 rounded-lg bg-white border border-gray-200 hover:bg-gray-50 dark:bg-gray-900 dark:border-gray-800"
    (click)="dropdownOpen = !dropdownOpen"
  >
    Actions
    <svg class="w-4 h-4 ml-2" [class.rotate-180]="dropdownOpen"><!-- Chevron --></svg>
  </button>

  <app-dropdown 
    [isOpen]="dropdownOpen" 
    (close)="dropdownOpen = false"
    class="absolute right-0 mt-2 w-48 rounded-lg shadow-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700"
  >
    <app-dropdown-item>
      <button class="w-full text-left px-4 py-2 hover:bg-gray-50 dark:hover:bg-gray-700">
        Edit
      </button>
    </app-dropdown-item>
    <app-dropdown-item>
      <button class="w-full text-left px-4 py-2 hover:bg-gray-50 dark:hover:bg-gray-700">
        Delete
      </button>
    </app-dropdown-item>
  </app-dropdown>
</div>
```

### 10. Badge Component Usage

#### Badge Examples
```html
<!-- Success Badge -->
<app-badge variant="light" color="success" size="md">
  Active
</app-badge>

<!-- Error Badge with Icon -->
<app-badge 
  variant="solid" 
  color="error" 
  [startIcon]="errorIcon"
>
  Failed
</app-badge>

<!-- Custom Styling -->
<app-badge 
  variant="light" 
  color="info" 
  className="custom-class"
>
  Pending
</app-badge>
```

## Tailwind CSS Custom Configuration

### Custom Theme Variables (in styles.css)
```css
@theme {
  --color-brand-500: #465fff;
  --color-success-500: #12b76a;
  --color-error-500: #f04438;
  --color-warning-500: #f79009;
  
  --shadow-theme-xs: 0px 1px 2px 0px rgba(16, 24, 40, 0.05);
  --shadow-theme-sm: 0px 1px 3px 0px rgba(16, 24, 40, 0.1);
  
  --breakpoint-2xsm: 375px;
  --breakpoint-xsm: 425px;
}
```

### Custom Utility Classes
```css
@utility menu-item {
  @apply relative flex items-center w-full gap-3 px-3 py-2 font-medium rounded-lg text-theme-sm;
}

@utility menu-item-active {
  @apply bg-brand-50 text-brand-500 dark:bg-brand-500/[0.12] dark:text-brand-400;
}

@utility no-scrollbar {
  &::-webkit-scrollbar {
    display: none;
  }
  -ms-overflow-style: none;
  scrollbar-width: none;
}
```

## Service Integration

### Sidebar Service
```typescript
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SidebarService {
  private isExpandedSubject = new BehaviorSubject(false);
  private isHoveredSubject = new BehaviorSubject(false);
  private isMobileOpenSubject = new BehaviorSubject(false);

  isExpanded$ = this.isExpandedSubject.asObservable();
  isHovered$ = this.isHoveredSubject.asObservable();
  isMobileOpen$ = this.isMobileOpenSubject.asObservable();

  toggleExpanded() {
    this.isExpandedSubject.next(!this.isExpandedSubject.value);
  }

  toggleMobileOpen() {
    this.isMobileOpenSubject.next(!this.isMobileOpenSubject.value);
  }

  setHovered(hovered: boolean) {
    this.isHoveredSubject.next(hovered);
  }

  setMobileOpen(open: boolean) {
    this.isMobileOpenSubject.next(open);
  }
}
```

## Component Integration Checklist

- [ ] Import CommonModule in component
- [ ] Use standalone component pattern
- [ ] Add @Input() props with types
- [ ] Add @Output() events for interactions
- [ ] Include className prop for customization
- [ ] Support dark mode via dark: classes
- [ ] Add hover and focus states
- [ ] Handle disabled states
- [ ] Use SafeHtmlPipe for SVG icons
- [ ] Test responsive behavior
- [ ] Document component props
- [ ] Create template files (.html)
- [ ] Add to module exports if needed
- [ ] Test keyboard navigation

