import { Component, EventEmitter, Input, Output, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-input-field',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="relative">
      <input
        [type]="type"
        [placeholder]="placeholder"
        [value]="value"
        [disabled]="disabled"
        [ngClass]="inputClasses"
        (input)="onInput($event)"
        (blur)="onTouched()"
      />
      @if (hint) {
      <p class="mt-1.5 text-xs"
        [ngClass]="{
          'text-error-500': error,
          'text-success-500': success,
          'text-gray-500 dark:text-gray-400': !error && !success
        }">
        {{ hint }}
      </p>
      }
    </div>
  `,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputFieldComponent),
      multi: true
    }
  ]
})
export class InputFieldComponent implements ControlValueAccessor {
  @Input() type: string = 'text';
  @Input() placeholder: string = '';
  @Input() error: boolean = false;
  @Input() success: boolean = false;
  @Input() hint?: string;
  @Output() valueChange = new EventEmitter<string>();

  value: string = '';
  disabled: boolean = false;

  get inputClasses(): string {
    let classes = 'h-11 w-full rounded-lg border appearance-none px-4 py-2.5 text-sm shadow-theme-xs placeholder:text-gray-400 focus:outline-hidden focus:ring-3 dark:bg-gray-900 dark:text-white/90 dark:placeholder:text-white/30';

    if (this.disabled) {
      classes += ' text-gray-500 border-gray-300 opacity-40 bg-gray-100 cursor-not-allowed dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700';
    } else if (this.error) {
      classes += ' border-error-500 focus:border-error-300 focus:ring-error-500/20 dark:text-error-400 dark:border-error-500 dark:focus:border-error-800';
    } else if (this.success) {
      classes += ' border-success-500 focus:border-success-300 focus:ring-success-500/20 dark:text-success-400 dark:border-success-500 dark:focus:border-success-800';
    } else {
      classes += ' bg-transparent text-gray-800 border-gray-300 focus:border-brand-300 focus:ring-brand-500/20 dark:border-gray-700 dark:text-white/90 dark:focus:border-brand-800';
    }
    
    return classes;
  }

  // ControlValueAccessor callbacks
  onChange: (value: string) => void = () => {};
  onTouched: () => void = () => {};

  // Called when form control value changes
  writeValue(value: string): void {
    this.value = value || '';
  }

  // Register callback for value changes
  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  // Register callback for touch events
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  // Called when form control is disabled/enabled
  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  // Handle input changes
  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.value = input.value;
    this.onChange(this.value);
    this.valueChange.emit(this.value);
  }
}
