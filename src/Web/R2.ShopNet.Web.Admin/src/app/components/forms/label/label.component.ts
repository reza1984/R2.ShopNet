import { Component } from '@angular/core';

@Component({
  selector: 'app-label',
  standalone: true,
  template: `
    <label class="block mb-2 text-sm font-medium text-gray-700 dark:text-gray-300">
      <ng-content></ng-content>
    </label>
  `
})
export class LabelComponent {}
