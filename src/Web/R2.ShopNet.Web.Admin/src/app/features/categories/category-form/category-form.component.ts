
import { Component, input, signal, effect, inject } from '@angular/core';
import { CategoryService } from '../category.service';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { InputFieldComponent } from '../../../components/forms/input-field/input-field.component';
import { LabelComponent } from '../../../components/forms/label/label.component';
import { PageBreadcrumbComponent } from '../../../components/ui/page-breadcrumb/page-breadcrumb.component';
import { ButtonComponent } from '../../../components/forms/button/button.component';
import { AlertComponent } from '../../../components/ui/alert/alert.component';
import { IconComponent } from '../../../components/icon/icon.component';
import { SelectComponent } from '../../../components/forms/select/select.component';
import { environment } from '../../../../environments/environment';
import { SlugHelper } from '../../../core/utils/slug.helper';

@Component({
	selector: 'app-category-form',
	templateUrl: './category-form.component.html',
	imports: [
		CommonModule,
		ReactiveFormsModule,
		InputFieldComponent,
        LabelComponent,
		PageBreadcrumbComponent,
		ButtonComponent,
		AlertComponent,
		IconComponent,
		SelectComponent,
	]
})
export class CategoryFormComponent {
	// Inject services
	private categoryService = inject(CategoryService);
	private route = inject(ActivatedRoute);
	public router = inject(Router);

	// Input signal
	categoryId = signal<string|undefined>(undefined);

	// State signals
	loading = signal(false);
	isEdit = signal(false);
	errorMessage = signal('');
	autoGenerateSlug = signal(true); // Track whether to auto-generate slug
	selectedImage = signal<File | null>(null);
	imagePreviewUrl = signal<string | null>(null);

	// Categories API URL for select component
	categoriesApiUrl = `${environment.apiUrl}/api/catalog/Categories`;

	// Form (not a signal, as FormGroup has its own reactivity)
	form: FormGroup;
	categories: Array<{ value: string; label: string }> = [];

	constructor() {
		// Initialize form
		this.form = new FormGroup({
			name: new FormControl('', Validators.required),
			slug: new FormControl('', Validators.required),
			description: new FormControl(''),
			parentCategoryId: new FormControl(''),
			displayOrder: new FormControl(0, [Validators.required, Validators.min(0)])
		});

		// Auto-generate slug from name when name changes
		this.form.get('name')?.valueChanges.subscribe(name => {
			if (this.autoGenerateSlug() && name) {
				this.form.get('slug')?.setValue(SlugHelper.generate(name), { emitEvent: false });
			}
		});

		// Disable auto-generation if user manually edits the slug
		this.form.get('slug')?.valueChanges.subscribe(() => {
			// If the user manually types in the slug field, disable auto-generation
			if (document.activeElement === document.querySelector('input[formControlName="slug"]')) {
				this.autoGenerateSlug.set(false);
			}
		});

		// Effect to load category when categoryId changes
		effect(() => {
			const id = this.categoryId();
			if (id) {
				this.isEdit.set(true);
				this.loadCategory(id);
			} else {
				const routeId = this.route.snapshot.paramMap.get('id');
				if (routeId) {
					this.isEdit.set(true);
					this.loadCategory(routeId);
				}
			}
		});
	}

	loadCategory(id: string): void {
		this.loading.set(true);
		this.categoryService.getCategoryById(id).subscribe({
			next: cat => {
				// Disable auto-generation when loading existing category
				this.autoGenerateSlug.set(false);
				this.categoryId.set(id);
				this.form.patchValue({
					name: cat.name,
					slug: cat.slug,
					description: cat.description,
					parentCategoryId: cat.parentCategoryId,
					displayOrder: cat.displayOrder
				});

				// Set image preview if category has an image
				if (cat.imageUrl) {
					this.imagePreviewUrl.set(cat.imageUrl);
				}

				this.loading.set(false);
				// Set the parent category option for display
				if(cat.parentCategoryId) {
					this.categories = [{ value: cat.parentCategoryId, label: cat.parentCategoryName! }];
				}
			},
			error: () => { this.loading.set(false); }
		});
	}

	saveCategory(): void {
		if (this.form.invalid) return;
		this.loading.set(true);
		this.errorMessage.set('');

		// Create FormData for file upload
		const formData = new FormData();
		formData.append('name', this.form.value.name);
		formData.append('slug', this.form.value.slug);
		formData.append('displayOrder', this.form.value.displayOrder.toString());

		// Add optional fields only if they have values
		if (this.form.value.description) {
			formData.append('description', this.form.value.description);
		}
		if (this.form.value.parentCategoryId) {
			formData.append('parentCategoryId', this.form.value.parentCategoryId);
		}

		// Add image file if selected
		if (this.selectedImage()) {
			formData.append('image', this.selectedImage()!);
		}

		if (this.isEdit() && this.categoryId()) {
			this.categoryService.updateCategory(this.categoryId()!, formData).subscribe({
				next: () => {
					this.loading.set(false);
					this.router.navigate(['/catalog/categories']);
				},
				error: (err) => {
					this.loading.set(false);
					console.error('Save error:', err);
					this.errorMessage.set(err.error?.message || err.message || 'Failed to save category. Please try again.');
					window.scrollTo({ top: 0, behavior: 'smooth' });
				}
			});
		} else {
			this.categoryService.createCategory(formData).subscribe({
				next: () => {
					this.loading.set(false);
					this.router.navigate(['/catalog/categories']);
				},
				error: (err) => {
					this.loading.set(false);
					console.error('Save error:', err);
					this.errorMessage.set(err.error?.message || err.message || 'Failed to save category. Please try again.');
					window.scrollTo({ top: 0, behavior: 'smooth' });
				}
			});
		}
	}

	onCancel(): void {
		this.router.navigate(['/catalog/categories']);
	}

	/**
	 * Manually regenerate slug from the current name value
	 * Useful if user wants to reset the slug after manual edits
	 */
	regenerateSlug(): void {
		const nameValue = this.form.get('name')?.value;
		if (nameValue) {
			this.form.get('slug')?.setValue(SlugHelper.generate(nameValue));
			this.autoGenerateSlug.set(true);
		}
	}

	/**
	 * Handle file selection from the file input
	 */
	onFileSelected(event: Event): void {
		const input = event.target as HTMLInputElement;
		if (input.files && input.files.length > 0) {
			const file = input.files[0];

			// Validate file type
			const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp', 'image/gif', 'image/svg+xml'];
			if (!allowedTypes.includes(file.type)) {
				this.errorMessage.set('Please select a valid image file (JPEG, PNG, WebP, GIF, or SVG).');
				return;
			}

			// Validate file size (5MB max)
			const maxSize = 5 * 1024 * 1024; // 5MB
			if (file.size > maxSize) {
				this.errorMessage.set('Image file size must be less than 5MB.');
				return;
			}

			this.selectedImage.set(file);
			this.errorMessage.set('');

			// Create preview URL
			const reader = new FileReader();
			reader.onload = (e) => {
				this.imagePreviewUrl.set(e.target?.result as string);
			};
			reader.readAsDataURL(file);
		}
	}

	/**
	 * Remove the selected image
	 */
	removeImage(): void {
		this.selectedImage.set(null);
		this.imagePreviewUrl.set(null);
		// Reset the file input
		const fileInput = document.getElementById('category-image') as HTMLInputElement;
		if (fileInput) {
			fileInput.value = '';
		}
	}
}
