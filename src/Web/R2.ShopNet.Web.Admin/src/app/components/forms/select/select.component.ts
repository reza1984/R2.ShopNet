import { CommonModule } from '@angular/common';
import { Component, input, output, effect, signal, inject, forwardRef, computed, DestroyRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { UuidHelper } from '../../../core/utils/uuid.helper';

export interface Option {
  value: string;
  label: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/**
 * SelectComponent - A feature-rich select dropdown component
 *
 * Features:
 * - Angular Forms compatible (implements ControlValueAccessor)
 * - Signal-based reactive state management
 * - API integration with pagination support
 * - Searchable with client-side and server-side filtering
 * - Debounced search (300ms default) to reduce API calls
 * - Clearable option to reset selection
 * - Supports both static options and dynamic API loading
 * - Disabled state support (via input or form control)
 *
 * Usage with Reactive Forms:
 * ```typescript
 * // In component:
 * form = new FormGroup({
 *   categoryId: new FormControl('')
 * });
 *
 * // In template:
 * <app-select formControlName="categoryId" [options]="categories" />
 *
 * // With API and all features:
 * <app-select
 *   formControlName="categoryId"
 *   url="http://localhost:5001/api/categories"
 *   valueField="id"
 *   labelField="name"
 *   [searchable]="true"
 *   [clearable]="true"
 *   [isDisabled]="false"
 *   [pageSize]="20"
 *   [debounceTime]="300"
 *   placeholder="Select a category"
 * />
 *
 * // Disabled via form control:
 * this.form.get('categoryId')?.disable();
 * ```
 */
@Component({
  selector: 'app-select',
  imports:[CommonModule],
  templateUrl: './select.component.html',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectComponent),
      multi: true
    }
  ]
})
export class SelectComponent implements ControlValueAccessor {
  private http = inject(HttpClient);
  private destroyRef = inject(DestroyRef);

  // Inputs
  options = input<Option[]>([]);
  placeholder = input<string>('Select an option');
  className = input<string>('');
  defaultValue = input<string>('');
  value = input<string>('');
  url = input<string>(''); // API URL for fetching options
  valueField = input<string>('id'); // Field to use as option value
  labelField = input<string>('name'); // Field to use as option label
  pageSize = input<number>(20); // Items per page
  searchable = input<boolean>(false); // Enable search functionality
  clearable = input<boolean>(false); // Enable clear button
  isDisabled = input<boolean>(false); // Disable the select
  debounceTime = input<number>(300); // Debounce time for search in milliseconds

  // Outputs
  valueChange = output<string>();

  // State
  selectedValue = signal<string>('');
  dynamicOptions = signal<Option[]>([]);
  isLoading = signal<boolean>(false);
  currentPage = signal<number>(1);
  hasMorePages = signal<boolean>(false);
  isOpen = signal<boolean>(false);
  searchTerm = signal<string>('');
  private disabledFromForm = signal<boolean>(false);

  // Search subject for debouncing
  private searchSubject$ = new Subject<string>();

  // Computed: Check if component is disabled (from input or form control)
  disabled = computed(() => this.isDisabled() || this.disabledFromForm());

  // ControlValueAccessor callbacks
  private onChangeFn: (value: string) => void = () => {};
  private onTouchedFn: () => void = () => {};

  id = `select-${UuidHelper.generate()}`;
  constructor() {
    // Initialize with default value or value input
    effect(() => {
      const val = this.value();
      const defaultVal = this.defaultValue();
      if (val) {
        this.selectedValue.set(val);
      } else if (defaultVal && !this.selectedValue()) {
        this.selectedValue.set(defaultVal);
      }
    });

    // Initialize dynamic options with static options when they change
    // This ensures options are available even before the dropdown is opened
    effect(() => {
      const staticOptions = this.options();
      const apiUrl = this.url();

      // If we have static options and are using API mode
      if (staticOptions.length > 0 && apiUrl) {
        // Only update if static options have actually changed
        const currentDynamic = this.dynamicOptions();

        // Check if we need to add any new static options
        const needsUpdate = staticOptions.some(staticOpt =>
          !currentDynamic.some(dynOpt => dynOpt.value === staticOpt.value)
        );

        if (needsUpdate) {
          const mergedOptions = [...staticOptions];
          currentDynamic.forEach(dynOpt => {
            if (!mergedOptions.some(opt => opt.value === dynOpt.value)) {
              mergedOptions.push(dynOpt);
            }
          });

          this.dynamicOptions.set(mergedOptions);
        }
      }
    });

    // Set up debounced search
    this.searchSubject$
      .pipe(
        debounceTime(this.debounceTime()),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((searchTerm) => {
        // If using API, reload options with search term
        if (this.url()) {
          this.loadOptions(1, searchTerm);
        }
      });
  }

  // ControlValueAccessor implementation
  writeValue(value: string): void {
    this.selectedValue.set(value || '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChangeFn = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouchedFn = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabledFromForm.set(isDisabled);
  }

  // Get all options (static or dynamic)
  getAllOptions(): Option[] {
    const staticOptions = this.options();
    const apiUrl = this.url();
    const search = this.searchTerm().toLowerCase();

    let allOptions = apiUrl ? this.dynamicOptions() : staticOptions;

    // Filter by search term if searchable is enabled
    if (this.searchable() && search) {
      allOptions = allOptions.filter(option =>
        option.label.toLowerCase().includes(search) ||
        option.value.toLowerCase().includes(search)
      );
    }

    return allOptions;
  }

  // Handle select open
  onOpen() {
    this.isOpen.set(true);
    const apiUrl = this.url();

    // Load data from API if URL is provided and no data loaded yet
    if (apiUrl && this.dynamicOptions().length === 0) {
      // First, initialize with static options if provided
      const staticOptions = this.options();
      if (staticOptions.length > 0) {
        this.dynamicOptions.set([...staticOptions]);
      }
      this.loadOptions(1);
    }
  }

  // Toggle dropdown for searchable select
  toggleDropdown() {
    if (this.isOpen()) {
      this.isOpen.set(false);
      this.onTouchedFn();
    } else {
      this.onOpen();
    }
  }

  // Get label for selected value
  getSelectedLabel(): string {
    const selected = this.getAllOptions().find(opt => opt.value === this.selectedValue());
    return selected ? selected.label : '';
  }

  // Select an option from dropdown
  selectOption(value: string) {
    this.selectedValue.set(value);
    this.valueChange.emit(value);
    this.onChangeFn(value);
    this.isOpen.set(false);
    this.searchTerm.set(''); // Clear search when selecting
    this.onTouchedFn();
  }

  // Handle select blur
  onSelectBlur(event: FocusEvent) {
    // Use setTimeout to allow click events on search input and buttons to fire first
    setTimeout(() => {
      // Only close if focus moved outside the component
      const relatedTarget = event.relatedTarget as HTMLElement;
      if (!relatedTarget || !relatedTarget.closest('.relative.w-full')) {
        this.isOpen.set(false);
        this.onTouchedFn();
      }
    }, 150);
  }

  // Handle search input focus
  onSearchFocus() {
    // Keep the dropdown open when search input is focused
    this.isOpen.set(true);
  }

  // Handle search input blur
  onSearchBlur() {
    // Delay closing to allow clicks on clear button
    setTimeout(() => {
      // Check if focus is still within the component
      const activeElement = document.activeElement;
      if (activeElement && !activeElement.closest('.relative.w-full')) {
        this.isOpen.set(false);
        this.onTouchedFn();
      }
    }, 150);
  }

  // Load options from API
  private loadOptions(page: number, search?: string) {
    const apiUrl = this.url();
    if (!apiUrl || this.isLoading()) return;

    this.isLoading.set(true);

    let params = new HttpParams()
      .set('pageNumber', page.toString())
      .set('pageSize', this.pageSize().toString());

    // Add search term if provided
    if (search) {
      params = params.set('search', search);
    }

    this.http.get<PaginatedResponse<any>>(apiUrl, { params }).subscribe({
      next: (response) => {
        const newOptions = response.items.map(item => ({
          value: item[this.valueField()],
          label: item[this.labelField()]
        }));

        if (page === 1) {
          // Preserve any static options that were set initially (like pre-selected parent)
          const staticOptions = this.options();

          // Merge static options with new options, avoiding duplicates
          const mergedOptions = [...staticOptions];
          newOptions.forEach(newOpt => {
            if (!mergedOptions.some(opt => opt.value === newOpt.value)) {
              mergedOptions.push(newOpt);
            }
          });

          this.dynamicOptions.set(mergedOptions);
        } else {
          this.dynamicOptions.update(opts => [...opts, ...newOptions]);
        }

        this.currentPage.set(page);
        this.hasMorePages.set(response.hasNextPage);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading options:', error);
        this.isLoading.set(false);
      }
    });
  }

  // Handle scroll event
  onScroll(event: Event) {
    const target = event.target as HTMLSelectElement;
    const scrollPosition = target.scrollTop + target.clientHeight;
    const scrollHeight = target.scrollHeight;

    // Load more when scrolled to bottom
    if (scrollPosition >= scrollHeight - 10 && this.hasMorePages() && !this.isLoading()) {
      this.loadOptions(this.currentPage() + 1);
    }
  }

  // Handle value change
  onChange(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    this.selectedValue.set(value);
    this.valueChange.emit(value);
    this.onChangeFn(value);
  }

  // Handle search input
  onSearch(event: Event) {
    const search = (event.target as HTMLInputElement).value;
    this.searchTerm.set(search);

    // Emit to subject for debounced API call
    this.searchSubject$.next(search);
  }

  // Clear search
  clearSearch() {
    this.searchTerm.set('');

    // If using API, reload options without search term
    if (this.url()) {
      this.loadOptions(1);
    }
  }

  // Clear selected value
  clearValue(event?: Event) {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    this.selectedValue.set('');
    this.valueChange.emit('');
    this.onChangeFn('');
  }

  // TrackBy function for ngFor
  trackByValue(index: number, option: Option): string {
    return option.value;
  }
}