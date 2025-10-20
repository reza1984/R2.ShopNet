import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SafeHtmlPipe } from '../../../../core/pipes/safe-html.pipe';
import { IconComponent } from '../../../components/icon/icon.component';

@Component({
  selector: 'app-alert',
  imports: [
    CommonModule,
    SafeHtmlPipe,
    RouterModule,
    IconComponent,
  ],
  templateUrl: './alert.component.html',
  styles: ``
})
export class AlertComponent {

  @Input() variant: 'success' | 'error' | 'warning' | 'info' = 'info';
  @Input() title: string = '';
  @Input() message: string = '';
  @Input() showLink: boolean = false;
  @Input() linkHref: string = '#';
  @Input() linkText: string = 'Learn more';

  get variantClasses() {
    return {
      success: {
        container: 'border-success-500 bg-success-50 dark:border-success-500/30 dark:bg-success-500/15',
        icon: 'text-success-500'
      },
      error: {
        container: 'border-error-500 bg-error-50 dark:border-error-500/30 dark:bg-error-500/15',
        icon: 'text-error-500'
      },
      warning: {
        container: 'border-warning-500 bg-warning-50 dark:border-warning-500/30 dark:bg-warning-500/15',
        icon: 'text-warning-500'
      },
      info: {
        container: 'border-blue-light-500 bg-blue-light-50 dark:border-blue-light-500/30 dark:bg-blue-light-500/15',
        icon: 'text-blue-light-500'
      }
    }[this.variant];
  }

  get iconName() {
    const icons = {
      success: 'check_circle',
      error: 'error',
      warning: 'warning',
      info: 'info'
    }
    return icons[this.variant];
  }
}
