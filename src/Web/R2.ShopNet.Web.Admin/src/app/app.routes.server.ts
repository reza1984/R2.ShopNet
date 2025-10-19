import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    // Dynamic routes with parameters should use Server-Side Rendering
    path: 'users/:id/edit',
    renderMode: RenderMode.Server
  },
  {
    // All other routes can be prerendered
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
