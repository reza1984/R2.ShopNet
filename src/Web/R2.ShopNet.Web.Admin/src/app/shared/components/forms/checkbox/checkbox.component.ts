import { Component, Input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-checkbox',
  standalone: true,
  template: `
    <div class="flex items-center gap-2">
      <input
        type="checkbox"
        [checked]="checked"
        [disabled]="disabled"
        (change)="onCheckboxChange($event)"
        (blur)="onTouched()"
        class="w-4 h-4 text-brand-500 border-gray-300 rounded cursor-pointer focus:ring-brand-500 dark:border-gray-700 dark:bg-gray-800 disabled:opacity-50 disabled:cursor-not-allowed"
      />
      @if (label) {
        <label class="text-sm font-normal cursor-pointer text-gray-700 dark:text-gray-400">
          {{ label }}
        </label>
      }
    </div>
  `,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CheckboxComponent),
      multi: true
    }
  ]
})
export class CheckboxComponent implements ControlValueAccessor {
  @Input() label: string = '';

  checked: boolean = false;
  disabled: boolean = false;

  // ControlValueAccessor callbacks
  onChange: (value: boolean) => void = () => {};
  onTouched: () => void = () => {};

  // Called when form control value changes
  writeValue(value: boolean): void {
    this.checked = !!value;
  }

  // Register callback for value changes
  registerOnChange(fn: (value: boolean) => void): void {
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

  // Handle checkbox changes
  onCheckboxChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.checked = input.checked;
    this.onChange(this.checked);
  }
}
