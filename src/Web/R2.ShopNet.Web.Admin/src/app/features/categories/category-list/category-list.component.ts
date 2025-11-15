import { Component, OnInit, signal } from '@angular/core';
import { CategoryService, Category } from '../category.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ButtonComponent } from '../../../components/forms/button/button.component';
import { IconComponent } from '../../../components/icon/icon.component';
import { AlertComponent } from '../../../components/ui/alert/alert.component';
import { ConfirmationModalComponent } from '../../../components/ui/confirmation-modal/confirmation-modal.component';

@Component({
	selector: 'app-category-list',
	templateUrl: './category-list.component.html',
	imports: [
		CommonModule,
		RouterModule,
		ButtonComponent,
		IconComponent,
		AlertComponent,
		ConfirmationModalComponent
	]
})
export class CategoryListComponent implements OnInit {
	categories = signal<Category[]>([]);
	totalCount = signal(0);
	pageNumber = signal(1);
	pageSize = signal(10);
	searchTerm = signal('');
	sortBy = signal('name');
	sortDescending = signal(false);
	parentCategoryId = signal<string | undefined>(undefined);
	loading = signal(false);
	errorMessage = signal<string>('');

	// Confirmation modal
	showDeleteConfirmation = signal(false);
	categoryToDelete = signal<string | null>(null);

	// Expose Math to template
	Math = Math;

	constructor(private categoryService: CategoryService) {}

	ngOnInit(): void {
		this.loadCategories();
	}

	loadCategories(): void {
		this.loading.set(true);
		
		this.categoryService.getCategories({
			pageNumber: this.pageNumber(),
			pageSize: this.pageSize(),
			parentCategoryId: this.parentCategoryId(),
			searchTerm: this.searchTerm(),
			sortBy: this.sortBy(),
			sortDescending: this.sortDescending()
		}).subscribe({
			next: res => {
				console.log('API Response:', res);
				this.categories.set(res.items);
				this.totalCount.set(res.totalCount);
				this.loading.set(false);
			},
			error: (err) => { 
				console.error('API Error:', err);
				this.loading.set(false);
			}
		});
	}

	confirmDeleteCategory(id: string): void {
		this.categoryToDelete.set(id);
		this.showDeleteConfirmation.set(true);
	}

	deleteCategory(): void {
		const id = this.categoryToDelete();
		if (!id) return;

		this.errorMessage.set('');
		this.showDeleteConfirmation.set(false);
		this.categoryService.deleteCategory(id).subscribe({
			next: () => this.loadCategories(),
			error: (err) => {
				console.error('Delete error:', err);
				this.errorMessage.set(err.error?.message || err.message || 'Failed to delete category. Please try again.');
				window.scrollTo({ top: 0, behavior: 'smooth' });
			}
		});
	}

	cancelDelete(): void {
		this.showDeleteConfirmation.set(false);
		this.categoryToDelete.set(null);
	}

	onSearch(term: string): void {
		this.searchTerm.set(term);
		this.pageNumber.set(1);
		this.loadCategories();
	}

	onPageChange(page: number): void {
		this.pageNumber.set(page);
		this.loadCategories();
	}

	onSortChange(sortBy: string, sortDescending: boolean): void {
		this.sortBy.set(sortBy);
		this.sortDescending.set(sortDescending);
		this.loadCategories();
	}

	// Optionally, load hierarchy
	loadHierarchy(): void {
		this.categoryService.getCategoryHierarchy().subscribe();
	}
}
