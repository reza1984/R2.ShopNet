# TailAdmin Dashboard Implementation Checklist

## Prerequisites

- [ ] Review [Admin-Dashboard-TailAdmin-Design.md](./Admin-Dashboard-TailAdmin-Design.md)
- [ ] Backup current Angular Material implementation
- [ ] Create feature branch: `feature/tailadmin-redesign`

## Phase 1: Setup & Configuration (Week 1)

### Day 1: Tailwind CSS Setup
- [ ] Install Tailwind CSS and dependencies
  ```bash
  npm install -D tailwindcss@latest postcss autoprefixer
  npm install -D @tailwindcss/forms @tailwindcss/typography
  npm install lucide-angular
  ```
- [ ] Initialize Tailwind configuration
  ```bash
  npx tailwindcss init
  ```
- [ ] Configure `tailwind.config.js` with brand colors
- [ ] Update `src/styles.scss` with Tailwind directives
- [ ] Remove or phase out Angular Material imports
- [ ] Test Tailwind utility classes working

### Day 2: Core Services
- [ ] Create `sidebar.service.ts` with signals
  - [ ] `isExpanded` signal
  - [ ] `isMobileOpen` signal
  - [ ] `isHovered` signal
  - [ ] Toggle methods
- [ ] Create `theme.service.ts`
  - [ ] `isDarkMode` signal
  - [ ] `toggleTheme()` method
  - [ ] localStorage persistence
  - [ ] Apply theme on init
- [ ] Create `navigation.model.ts` with NavItem types

### Day 3: App Layout Component
- [ ] Create `app-layout.component.ts`
- [ ] Create `app-layout.component.html`
  ```html
  <div class="min-h-screen xl:flex">
    <app-sidebar />
    <app-backdrop />
    <div class="flex-1">
      <app-header />
      <main>
        <router-outlet />
      </main>
    </div>
  </div>
  ```
- [ ] Add responsive margin classes based on sidebar state
- [ ] Add smooth transitions

### Day 4: Sidebar Component
- [ ] Create `app-sidebar.component.ts` in `shared/layout/`
- [ ] Create `app-sidebar.component.html`
- [ ] Implement collapsible behavior
  - [ ] Desktop: 290px expanded, 90px collapsed
  - [ ] Hover to expand when collapsed
  - [ ] Mobile: slide-in drawer
- [ ] Add logo section with conditional rendering
- [ ] Create navigation menu structure
  - [ ] Main menu section
  - [ ] Others section
  - [ ] Submenu dropdowns
- [ ] Style menu items with active states
- [ ] Add menu icons (SVG inline or Lucide)
- [ ] Add widget/promo section at bottom

### Day 5: Header & Backdrop
- [ ] Create `app-header.component.ts`
  - [ ] Hamburger menu button (mobile)
  - [ ] Search bar (optional)
  - [ ] Theme toggle button
  - [ ] Notifications dropdown
  - [ ] User profile dropdown
- [ ] Create `backdrop.component.ts`
  - [ ] Show on mobile when sidebar open
  - [ ] Click to close sidebar
  - [ ] z-index management
- [ ] Test mobile responsiveness
- [ ] Test sidebar interactions

## Phase 2: Common Components (Week 2)

### Day 6-7: UI Components
- [ ] Create `button.component.ts`
  - [ ] Variants: primary, secondary, danger, ghost
  - [ ] Sizes: sm, md, lg
  - [ ] Loading state with spinner
  - [ ] Icon support
- [ ] Create `badge.component.ts`
  - [ ] Status colors: success, warning, danger, info
  - [ ] Sizes: sm, md, lg
- [ ] Create `alert.component.ts`
  - [ ] Types: success, warning, danger, info
  - [ ] Dismissible option
  - [ ] Icon support
- [ ] Create `card.component.ts`
  - [ ] Header with title
  - [ ] Optional footer
  - [ ] Padding variants

### Day 8: Common Utilities
- [ ] Create `page-breadcrumb.component.ts`
  - [ ] Dynamic breadcrumb from route
  - [ ] Navigation support
- [ ] Create `safe-html.pipe.ts` for SVG icons
- [ ] Create `loading-spinner.component.ts`
- [ ] Create `empty-state.component.ts`

### Day 9: Header Dropdowns
- [ ] Create `theme-toggle.component.ts`
  - [ ] Sun/moon icon
  - [ ] Toggle dark mode
  - [ ] Smooth transition
- [ ] Create `notification-dropdown.component.ts`
  - [ ] Bell icon with badge
  - [ ] Notification list
  - [ ] Mark as read
  - [ ] View all link
- [ ] Create `user-dropdown.component.ts`
  - [ ] User avatar
  - [ ] Profile link
  - [ ] Settings link
  - [ ] Logout button

### Day 10: Table Components
- [ ] Create `data-table.component.ts`
  - [ ] Responsive wrapper
  - [ ] Sortable columns
  - [ ] Pagination controls
  - [ ] Row selection (optional)
  - [ ] Action column
- [ ] Create `pagination.component.ts`
  - [ ] Previous/Next buttons
  - [ ] Page numbers
  - [ ] Items per page dropdown

## Phase 3: Dashboard Pages (Week 3)

### Day 11-12: Main Dashboard
- [ ] Create `dashboard/ecommerce/ecommerce.component.ts`
- [ ] Create stats cards component
  - [ ] Total revenue
  - [ ] Total sales
  - [ ] Total users
  - [ ] Total products
  - [ ] Trend indicators
- [ ] Integrate chart library (Chart.js or ApexCharts)
  - [ ] Line chart for sales
  - [ ] Bar chart for revenue
  - [ ] Responsive charts
- [ ] Create recent orders widget
- [ ] Create top products widget
- [ ] Add activity timeline

### Day 13-14: User Management
- [ ] Update `user-list.component.ts`
  - [ ] Replace Material table with Tailwind table
  - [ ] Add search input with icon
  - [ ] Add filter dropdown (status)
  - [ ] Add action buttons (Edit, Delete, Toggle)
  - [ ] Add status badges
  - [ ] Update pagination UI
- [ ] Update `user-edit.component.ts`
  - [ ] Replace Material forms with Tailwind
  - [ ] Add form validation styling
  - [ ] Add save/cancel buttons
  - [ ] Add loading states
  - [ ] Add success/error messages

### Day 15: Profile Page
- [ ] Create `profile/profile.component.ts`
- [ ] Create profile header section
  - [ ] Avatar upload
  - [ ] User name and role
  - [ ] Action buttons
- [ ] Create about section
  - [ ] Personal info cards
  - [ ] Edit mode
- [ ] Create activity section
  - [ ] Timeline of recent actions
  - [ ] Filters

## Phase 4: Additional Features (Week 4)

### Day 16-17: Forms & Tables
- [ ] Create `form-elements/form-elements.component.ts`
  - [ ] Text inputs
  - [ ] Select dropdowns
  - [ ] Checkboxes
  - [ ] Radio buttons
  - [ ] Textareas
  - [ ] File upload
  - [ ] Date pickers
- [ ] Create `basic-tables/basic-tables.component.ts`
  - [ ] Simple table
  - [ ] Striped table
  - [ ] Bordered table
  - [ ] Hover effects

### Day 18: Charts Page
- [ ] Create `line-chart/line-chart.component.ts`
  - [ ] Multiple line charts
  - [ ] Responsive
  - [ ] Dark mode support
- [ ] Create `bar-chart/bar-chart.component.ts`
  - [ ] Multiple bar charts
  - [ ] Horizontal/vertical
  - [ ] Stacked option

### Day 19: Authentication Pages
- [ ] Create `auth/sign-in/sign-in.component.ts`
  - [ ] Use `auth-page-layout`
  - [ ] Email/password form
  - [ ] Remember me checkbox
  - [ ] Forgot password link
  - [ ] Sign up link
- [ ] Create `auth/sign-up/sign-up.component.ts`
  - [ ] Registration form
  - [ ] Terms acceptance
  - [ ] Sign in link

### Day 20: Error & Blank Pages
- [ ] Create `not-found/not-found.component.ts`
  - [ ] 404 illustration
  - [ ] Error message
  - [ ] Back home button
- [ ] Create `blank/blank.component.ts`
  - [ ] Empty template
  - [ ] Breadcrumb

## Phase 5: Polish & Testing (Week 5)

### Day 21: Dark Mode Refinement
- [ ] Test all components in dark mode
- [ ] Fix any contrast issues
- [ ] Ensure smooth theme transitions
- [ ] Verify icons visibility
- [ ] Test form inputs styling

### Day 22: Mobile Responsive Testing
- [ ] Test on iPhone (375px)
- [ ] Test on iPad (768px)
- [ ] Test on Desktop (1280px+)
- [ ] Verify sidebar drawer works
- [ ] Check table scrolling
- [ ] Test form layouts
- [ ] Verify navigation dropdowns

### Day 23: Accessibility Audit
- [ ] Run Lighthouse accessibility scan
- [ ] Add ARIA labels where needed
- [ ] Test keyboard navigation
  - [ ] Tab through all interactive elements
  - [ ] Escape to close dropdowns
  - [ ] Enter to activate buttons
- [ ] Verify focus indicators visible
- [ ] Test with screen reader (NVDA/JAWS)
- [ ] Check color contrast ratios (WCAG AA)

### Day 24: Performance Optimization
- [ ] Run Lighthouse performance scan
- [ ] Optimize images (WebP, lazy loading)
- [ ] Enable Tailwind CSS purging
- [ ] Review bundle size
- [ ] Test SSR rendering
- [ ] Optimize signal updates
- [ ] Check for memory leaks

### Day 25: Documentation
- [ ] Update README.md
- [ ] Document component API
- [ ] Add Storybook/examples (optional)
- [ ] Create style guide
- [ ] Document theme customization
- [ ] Add troubleshooting guide

## Validation Checklist

### Functional Testing
- [ ] All routes work correctly
- [ ] Navigation menu expands/collapses
- [ ] Dark mode toggles properly
- [ ] Forms submit correctly
- [ ] Tables sort and paginate
- [ ] Dropdowns open/close
- [ ] Mobile menu works
- [ ] Search functionality works
- [ ] Authentication flow works

### Visual Testing
- [ ] Design matches TailAdmin reference
- [ ] Colors consistent across pages
- [ ] Typography hierarchy clear
- [ ] Spacing consistent
- [ ] Icons render correctly
- [ ] Animations smooth
- [ ] No layout shifts
- [ ] Responsive at all breakpoints

### Browser Testing
- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] Mobile Safari (iOS)
- [ ] Chrome Mobile (Android)

### Performance Metrics
- [ ] First Contentful Paint < 1.5s
- [ ] Largest Contentful Paint < 2.5s
- [ ] Time to Interactive < 3.5s
- [ ] Cumulative Layout Shift < 0.1
- [ ] Bundle size < 500KB (gzipped)

## Deployment Checklist

- [ ] Run production build
  ```bash
  npm run build
  ```
- [ ] Test production build locally
  ```bash
  npm run serve:ssr:R2.ShopNet.Web.Admin
  ```
- [ ] Run all tests
  ```bash
  npm test
  ```
- [ ] Review PR with team
- [ ] Merge to main branch
- [ ] Deploy to staging
- [ ] User acceptance testing
- [ ] Deploy to production
- [ ] Monitor for errors

## Migration Notes

### Removing Angular Material

Components to replace:
- `MatToolbarModule` → Custom header with Tailwind
- `MatSidenavModule` → Custom sidebar component
- `MatButtonModule` → Custom button component
- `MatIconModule` → Lucide icons or SVG
- `MatTableModule` → Custom table with Tailwind
- `MatPaginatorModule` → Custom pagination
- `MatFormFieldModule` → Tailwind form controls
- `MatInputModule` → Native inputs with Tailwind
- `MatSelectModule` → Custom select dropdown
- `MatDialogModule` → Custom modal (if needed)
- `MatSnackBarModule` → Custom toast notifications

### CSS Migration

1. Replace Material theme imports with Tailwind
2. Remove `@angular/material/prebuilt-themes`
3. Update custom styles to Tailwind utilities
4. Convert SCSS mixins to CSS custom properties

### Breaking Changes

- All Material components removed
- CSS class names changed
- Some component APIs changed
- Theme configuration different

---

**Total Estimated Time**: 25 days (5 weeks)  
**Team Size**: 1-2 developers  
**Status**: 📋 Ready to Start
