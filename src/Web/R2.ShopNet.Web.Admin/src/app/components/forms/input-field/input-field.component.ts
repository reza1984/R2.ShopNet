import { Component, EventEmitter, Input, Output, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-input-field',
  standalone: true,
  template: `
    <input
      [type]="type"
      [placeholder]="placeholder"
      [value]="value"
      [disabled]="disabled"
      (input)="onInput($event)"
      (blur)="onTouched()"
      class="w-full px-4 py-3 text-sm transition bg-white border border-gray-300 rounded-lg placeholder:text-gray-400 text-gray-900 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 dark:bg-gray-800 dark:border-gray-700 dark:text-white dark:focus:border-brand-400 disabled:opacity-50 disabled:cursor-not-allowed"
    />
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
  @Output() valueChange = new EventEmitter<string>();

  value: string = '';
  disabled: boolean = false;

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
