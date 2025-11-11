import { Component, input } from '@angular/core';
import { RouterModule } from '@angular/router';

export interface BreadcrumbParent {
  title: string;
  url: string;
}

@Component({
  selector: 'app-page-breadcrumb',
  imports: [
    RouterModule,
  ],
  templateUrl: './page-breadcrumb.component.html',
  styles: ``
})
export class PageBreadcrumbComponent {
  pageTitle = input.required<string>();
  parent = input<BreadcrumbParent>();
}
