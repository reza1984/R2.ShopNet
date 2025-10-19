# Google Material Icons Usage Guide

## 🎨 Available in Your App

You now have access to **Material Icons** and **Material Symbols** from Google Fonts.

## 📖 How to Use

### Option 1: Direct HTML (Simple)

```html
<!-- Material Icons (Classic) -->
<span class="material-icons">home</span>
<span class="material-icons">settings</span>
<span class="material-icons">shopping_cart</span>

<!-- Material Symbols (New, Rounded) -->
<span class="material-symbols-rounded">home</span>
<span class="material-symbols-rounded">settings</span>
<span class="material-symbols-rounded">shopping_cart</span>

<!-- Filled variant -->
<span class="material-symbols-rounded material-symbols-filled">favorite</span>
```

### Option 2: Using the Icon Component (Recommended)

```typescript
import { IconComponent } from '@shared/components/icon/icon.component';

@Component({
  imports: [IconComponent]
})
```

```html
<!-- Basic usage -->
<app-icon name="home"></app-icon>

<!-- Custom size and color -->
<app-icon name="settings" [size]="32" color="#465fff"></app-icon>

<!-- Filled style -->
<app-icon name="favorite" [filled]="true"></app-icon>

<!-- Different styles -->
<app-icon name="search" style="material"></app-icon>
<app-icon name="search" style="symbols-rounded"></app-icon>
```

### Option 3: Inline with Tailwind Classes

```html
<span class="material-symbols-rounded text-2xl text-brand-500">
  account_circle
</span>

<button class="flex items-center gap-2">
  <span class="material-symbols-rounded">add</span>
  Add User
</button>
```

## 🔍 Finding Icons

1. **Browse Icons**: Visit [Google Fonts Icons](https://fonts.google.com/icons)
2. **Search**: Type what you need (e.g., "user", "settings", "dashboard")
3. **Copy Name**: Click the icon and copy its name (e.g., `person`, `settings`, `dashboard`)
4. **Use**: Paste the name in your HTML/component

## ✨ Popular Icons for Your App

### Navigation
- `dashboard` - Dashboard
- `people` / `group` - Users
- `inventory_2` - Products
- `receipt_long` - Orders
- `analytics` - Analytics
- `bar_chart` - Reports
- `settings` - Settings

### Actions
- `add` - Add new
- `edit` - Edit
- `delete` - Delete
- `search` - Search
- `filter_list` - Filter
- `download` - Export
- `upload` - Import
- `refresh` - Reload

### Status
- `check_circle` - Success
- `cancel` - Error
- `info` - Info
- `warning` - Warning
- `visibility` - View
- `visibility_off` - Hide

### UI Elements
- `menu` - Hamburger menu
- `close` - Close/X
- `arrow_back` - Back arrow
- `arrow_forward` - Forward arrow
- `expand_more` - Dropdown
- `chevron_right` - Right chevron
- `more_vert` - Three dots menu

## 🎯 Examples in Your Components

### User List Actions
```html
<button [routerLink]="['/users', user.id, 'edit']" 
        class="rounded-lg p-2 hover:bg-gray-100">
  <span class="material-symbols-rounded text-lg">edit</span>
</button>

<button (click)="deleteUser(user)" 
        class="rounded-lg p-2 text-error-600 hover:bg-error-50">
  <span class="material-symbols-rounded text-lg">delete</span>
</button>
```

### Header Actions
```html
<button (click)="onSearch()" 
        class="inline-flex items-center gap-2">
  <span class="material-symbols-rounded">search</span>
  Search
</button>

<button routerLink="/users/create" 
        class="inline-flex items-center gap-2 rounded-lg bg-brand-500 px-4 py-2.5">
  <span class="material-symbols-rounded">add</span>
  Add User
</button>
```

## 🎨 Styling Tips

### Size with Tailwind
```html
<span class="material-symbols-rounded text-sm">icon</span>  <!-- 14px -->
<span class="material-symbols-rounded text-base">icon</span> <!-- 16px -->
<span class="material-symbols-rounded text-lg">icon</span>   <!-- 18px -->
<span class="material-symbols-rounded text-xl">icon</span>   <!-- 20px -->
<span class="material-symbols-rounded text-2xl">icon</span>  <!-- 24px -->
<span class="material-symbols-rounded text-3xl">icon</span>  <!-- 30px -->
```

### Color with Tailwind
```html
<span class="material-symbols-rounded text-brand-500">icon</span>
<span class="material-symbols-rounded text-success-600">icon</span>
<span class="material-symbols-rounded text-error-600">icon</span>
<span class="material-symbols-rounded text-gray-400">icon</span>
```

### Custom CSS
```css
.icon-custom {
  font-size: 20px;
  color: #465fff;
  vertical-align: middle;
}
```

## 🚀 Quick Start Example

Replace your SVG icons in the user-list component:

**Before (SVG):**
```html
<svg class="size-5" viewBox="0 0 20 20" fill="none">
  <path d="M14.1667..." stroke="currentColor"/>
</svg>
```

**After (Material Icon):**
```html
<span class="material-symbols-rounded text-xl">edit</span>
```

**Benefits:**
- ✅ Smaller file size
- ✅ Easier to use
- ✅ Consistent styling
- ✅ 2000+ icons available
- ✅ Better accessibility

## 📚 Resources

- **Icon Library**: https://fonts.google.com/icons
- **Material Symbols Guide**: https://developers.google.com/fonts/docs/material_symbols
- **Icon Component**: `src/app/shared/components/icon/icon.component.ts`
