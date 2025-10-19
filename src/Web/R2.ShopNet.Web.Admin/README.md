# R2.ShopNet Admin Portal

Admin dashboard for managing users and system configuration in the R2.ShopNet e-commerce platform.

## Technology Stack

- **Angular 20** - Standalone components with signals
- **Angular Material 20** - UI component library
- **TypeScript 5.7+** - Type-safe development
- **Zoneless Change Detection** - Improved performance
- **Server-Side Rendering (SSR)** - Better SEO and initial load

## Features

### User Management
- ✅ List all users with pagination
- ✅ Search users by email or name
- ✅ Filter users by active status
- ✅ View user details and roles
- ✅ Edit user information (name, phone)
- ✅ Activate/Deactivate user accounts
- ✅ Soft delete users
- 🔄 Role assignment (coming soon)

## Getting Started

### Prerequisites
- Node.js 20.x or higher
- npm 10.x or higher
- Identity Service running on http://localhost:5001

### Installation
npm install

### Development
npm start
# App will be available at http://localhost:4200

### Build
npm run build

## API Integration

The admin portal connects to the Identity Service API:
- GET /api/users - Get paginated users list
- GET /api/users/{id} - Get user by ID
- PUT /api/users/{id} - Update user information
- DELETE /api/users/{id} - Soft delete user
- POST /api/users/{id}/activate - Activate user account
- POST /api/users/{id}/deactivate - Deactivate user account
