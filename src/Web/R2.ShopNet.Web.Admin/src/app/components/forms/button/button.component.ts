import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-button',
  standalone: true,
  template: `
    <button
      [type]="type"
      [class]="getButtonClasses()"
      (click)="onClick($event)"
      [disabled]="disabled"
    >
      <ng-content></ng-content>
    </button>
  `
})
export class ButtonComponent {
  @Input() type: 'button' | 'submit' = 'button';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() variant: 'primary' | 'secondary' | 'outline' = 'primary';
  @Input() className: string = '';
  @Input() disabled: boolean = false;
  @Output() btnClick = new EventEmitter<Event>();

  onClick(event: Event): void {
    if (!this.disabled) {
      this.btnClick.emit(event);
    }
  }

  getButtonClasses(): string {
    const baseClasses = 'inline-flex items-center justify-center font-medium transition rounded-lg focus:outline-none focus:ring-2 focus:ring-offset-2';

    const sizeClasses = {
      sm: 'px-4 py-3 text-sm',
      md: 'px-5 py-3 text-base',
      lg: 'px-6 py-4 text-lg'
    };

    const variantClasses = {
      primary: 'text-white bg-brand-500 hover:bg-brand-600 focus:ring-brand-500 shadow-theme-xs disabled:opacity-50 disabled:cursor-not-allowed',
      secondary: 'text-gray-700 bg-gray-200 hover:bg-gray-300 focus:ring-gray-500 dark:bg-gray-700 dark:text-white dark:hover:bg-gray-600',
      outline: 'text-brand-500 border border-brand-500 hover:bg-brand-50 focus:ring-brand-500 dark:hover:bg-brand-900/10'
    };

    return `${baseClasses} ${sizeClasses[this.size]} ${variantClasses[this.variant]} ${this.className}`;
  }
}
