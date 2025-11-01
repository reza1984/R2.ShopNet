# Quick Start: Tailwind Dashboard Implementation

This guide will help you quickly set up the Tailwind Dashboard for R2.ShopNet Admin Portal.

## Prerequisites

- Node.js 18+ installed
- Angular 20 project already created
- Basic understanding of Tailwind CSS

## Step 1: Install Dependencies

```bash
cd src/Web/R2.ShopNet.Web.Admin

# Install Tailwind CSS and plugins
npm install -D tailwindcss@latest postcss autoprefixer
npm install -D @tailwindcss/forms @tailwindcss/typography

# Install icon library
npm install lucide-angular

# Optional: Install chart library for dashboard
npm install chart.js ng2-charts
```

## Step 2: Initialize Tailwind

```bash
# Create Tailwind config
npx tailwindcss init

# This creates tailwind.config.js
```

## Step 3: Configure Tailwind

Update `tailwind.config.js`:

```javascript
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  darkMode: 'class', // Enable dark mode with class strategy
  theme: {
    extend: {
      colors: {
  // Brand colors matching Tailwind Dashboard
        brand: {
          50: '#eff3ff',
          100: '#dbe3fe',
          200: '#bfd0fe',
          300: '#93b1fd',
          400: '#6089fa',
          500: '#3c50e0', // Primary brand color
          600: '#284bc4',
          700: '#1f3ba0',
          800: '#1e3382',
          900: '#1d2e6b',
          950: '#161e41',
        },
      },
      maxWidth: {
        '(--breakpoint-2xl)': '1536px',
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
}
```

## Step 4: Update Styles

Replace content of `src/styles.scss`:

```scss
@tailwind base;
@tailwind components;
@tailwind utilities;

/* Custom utility classes */
@layer components {
  /* Menu item styles */
  .menu-item {
    @apply flex items-center gap-3 p-3 rounded-lg transition-all duration-200;
  }
  
  .menu-item-active {
    @apply bg-brand-50 text-brand-500 dark:bg-white/5 dark:text-brand-500;
  }
  
  .menu-item-inactive {
    @apply text-gray-700 hover:bg-gray-50 dark:text-gray-400 dark:hover:bg-white/5;
  }
  
  .menu-item-icon-size {
    @apply size-6 flex-shrink-0;
  }
  
  .menu-item-icon-active {
    @apply text-brand-500;
  }
  
  .menu-item-icon-inactive {
    @apply text-gray-500 group-hover:text-brand-500 dark:text-gray-400;
  }
  
  .menu-item-text {
    @apply flex-1 truncate;
  }
  
  /* Menu dropdown styles */
  .menu-dropdown-item {
    @apply flex items-center gap-2 rounded-lg px-4 py-2 text-sm transition-colors;
  }
  
  .menu-dropdown-item-active {
    @apply bg-brand-50 text-brand-500 dark:bg-white/5 dark:text-brand-500;
  }
  
  .menu-dropdown-item-inactive {
    @apply text-gray-600 hover:bg-gray-50 dark:text-gray-400 dark:hover:bg-white/5;
  }
  
  .menu-dropdown-badge {
    @apply rounded-md px-2 py-0.5 text-xs font-medium;
  }
  
  .menu-dropdown-badge-active {
    @apply bg-brand-500 text-white;
  }
  
  .menu-dropdown-badge-inactive {
    @apply bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-300;
  }
  
  /* Card styles */
  .card {
    @apply rounded-2xl border border-gray-200 bg-white p-6 dark:border-gray-800 dark:bg-gray-900;
  }
  
  /* Button styles */
  .btn-primary {
    @apply inline-flex items-center justify-center rounded-lg bg-brand-500 px-4 py-2.5 font-medium text-white transition-colors hover:bg-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-500/20 disabled:opacity-50 disabled:cursor-not-allowed;
  }
  
  .btn-secondary {
    @apply inline-flex items-center justify-center rounded-lg border border-gray-300 bg-white px-4 py-2.5 font-medium text-gray-700 transition-colors hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-300 dark:hover:bg-gray-700;
  }
  
  .btn-danger {
    @apply inline-flex items-center justify-center rounded-lg bg-red-500 px-4 py-2.5 font-medium text-white transition-colors hover:bg-red-600 focus:outline-none focus:ring-2 focus:ring-red-500/20;
  }
  
  /* Input styles */
  .input {
    @apply block w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-gray-900 transition-colors focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 disabled:opacity-50 disabled:cursor-not-allowed dark:border-gray-700 dark:bg-gray-800 dark:text-white dark:focus:border-brand-500;
  }
  
  /* Badge styles */
  .badge {
    @apply inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium;
  }
  
  .badge-success {
    @apply bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400;
  }
  
  .badge-warning {
    @apply bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400;
  }
  
  .badge-danger {
    @apply bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400;
  }
  
  .badge-info {
    @apply bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400;
  }
}

/* Scrollbar styles */
@layer utilities {
  .no-scrollbar::-webkit-scrollbar {
    display: none;
  }
  
  .no-scrollbar {
    -ms-overflow-style: none;
    scrollbar-width: none;
  }
}

/* Base styles */
@layer base {
  body {
    @apply bg-gray-50 text-gray-900 antialiased dark:bg-gray-950 dark:text-white;
  }
}
```

## Step 5: Update Angular Configuration

Update `angular.json` to include Tailwind CSS processing (if not already configured):

```json
{
  "projects": {
    "R2.ShopNet.Web.Admin": {
      "architect": {
        "build": {
          "options": {
            "styles": [
              "src/styles.scss"
            ]
          }
        }
      }
    }
  }
}
```

## Step 6: Create Core Services

Create `src/app/core/services/sidebar.service.ts`:

```typescript
import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SidebarService {
  // Signals for sidebar state
  private isExpandedSignal = signal(true);
  private isMobileOpenSignal = signal(false);
  private isHoveredSignal = signal(false);
  
  // Read-only observables
  readonly isExpanded$ = this.isExpandedSignal.asReadonly();
  readonly isMobileOpen$ = this.isMobileOpenSignal.asReadonly();
  readonly isHovered$ = this.isHoveredSignal.asReadonly();
  
  toggleExpanded() {
    this.isExpandedSignal.update(v => !v);
  }
  
  setExpanded(value: boolean) {
    this.isExpandedSignal.set(value);
  }
  
  setMobileOpen(value: boolean) {
    this.isMobileOpenSignal.set(value);
  }
  
  setHovered(value: boolean) {
    this.isHoveredSignal.set(value);
  }
}
```

Create `src/app/core/services/theme.service.ts`:

```typescript
import { Injectable, signal, effect } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private isDarkModeSignal = signal(false);
  readonly isDarkMode$ = this.isDarkModeSignal.asReadonly();
  
  constructor() {
    this.loadThemePreference();
    
    // Effect to apply theme whenever it changes
    effect(() => {
      this.applyTheme();
    });
  }
  
  toggleTheme() {
    this.isDarkModeSignal.update(v => !v);
  }
  
  setDarkMode(value: boolean) {
    this.isDarkModeSignal.set(value);
  }
  
  private loadThemePreference() {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
      this.isDarkModeSignal.set(true);
    } else if (savedTheme === 'light') {
      this.isDarkModeSignal.set(false);
    } else {
      // Check system preference
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      this.isDarkModeSignal.set(prefersDark);
    }
  }
  
  private applyTheme() {
    const isDark = this.isDarkModeSignal();
    if (isDark) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
    localStorage.setItem('theme', isDark ? 'dark' : 'light');
  }
}
```

## Step 7: Create Navigation Model

Create `src/app/core/models/navigation.model.ts`:

```typescript
export interface NavItem {
  name: string;
  icon: string; // SVG string
  path?: string;
  new?: boolean;
  subItems?: NavSubItem[];
}

export interface NavSubItem {
  name: string;
  path: string;
  pro?: boolean;
  new?: boolean;
}
```

## Step 8: Test the Setup

Start the development server:

```bash
npm start
```

Verify Tailwind is working by temporarily adding utility classes to `app.html`:

```html
<div class="bg-brand-500 text-white p-4 rounded-lg">
  Tailwind CSS is working!
</div>
```

## Step 9: Remove Angular Material (Optional)

If you want to completely remove Angular Material:

```bash
# Uninstall Material packages
npm uninstall @angular/material @angular/cdk
```

Update `app.ts` to remove Material imports:

```typescript
import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('R2.ShopNet Admin Portal');
}
```

## Step 10: Create First Component

Create a simple sidebar component to test:

```bash
mkdir -p src/app/shared/layout/app-sidebar
```

Create `src/app/shared/layout/app-sidebar/app-sidebar.component.ts`:

```typescript
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarService } from '../../../core/services/sidebar.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <aside 
      class="fixed top-0 left-0 h-screen bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 transition-all duration-300"
      [class.w-[290px]]="isExpanded$()"
      [class.w-[90px]]="!isExpanded$()">
      <div class="p-5">
        <h1 class="text-xl font-bold text-brand-500">Admin</h1>
      </div>
      <nav class="p-5">
        <button 
          (click)="toggleSidebar()"
          class="btn-primary w-full">
          Toggle Sidebar
        </button>
      </nav>
    </aside>
  `
})
export class AppSidebarComponent {
  readonly isExpanded$ = this.sidebarService.isExpanded$;
  
  constructor(private sidebarService: SidebarService) {}
  
  toggleSidebar() {
    this.sidebarService.toggleExpanded();
  }
}
```

## Next Steps

1. ✅ Tailwind CSS is configured
2. ✅ Core services are created
3. ✅ Basic sidebar component is created
4. 📋 Follow [Admin-Dashboard-Tailwind-Implementation-Checklist.md](./Admin-Dashboard-Tailwind-Implementation-Checklist.md) for full implementation
5. 📋 Reference [Admin-Dashboard-Tailwind-Design.md](./Admin-Dashboard-Tailwind-Design.md) for design details

## Troubleshooting

### Tailwind styles not applying
- Verify `content` paths in `tailwind.config.js`
- Check `styles.scss` has `@tailwind` directives
- Clear browser cache and restart dev server

### Dark mode not working
- Ensure `darkMode: 'class'` in `tailwind.config.js`
- Check `ThemeService` is applying 'dark' class to `<html>`
- Verify dark mode variants in component classes (e.g., `dark:bg-gray-900`)

### Icons not showing
- Install `lucide-angular` package
- Import icon components where needed
- Alternative: Use inline SVG strings

## Resources

- [Full Design Document](./Admin-Dashboard-Tailwind-Design.md)
- [Implementation Checklist](./Admin-Dashboard-Tailwind-Implementation-Checklist.md)
- [Tailwind Dashboard Demo](https://angular-demo.tailadmin.com/)
- [Tailwind CSS Documentation](https://tailwindcss.com/docs)

---

**Ready to build!** 🚀
