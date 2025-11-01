import { Component, OnInit } from '@angular/core';
import { CategoryService, Category } from '../category.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ButtonComponent } from '../../../components/forms/button/button.component';
import { InputFieldComponent } from '../../../components/forms/input-field/input-field.component';
import { AlertComponent } from '../../../components/ui/alert/alert.component';
import { DropdownItemComponent } from '../../../components/ui/dropdown/dropdown-item/dropdown-item.component';
import { DropdownComponent } from '../../../components/ui/dropdown/dropdown.component';
import { TableBodyComponent } from '../../../components/ui/table/table-body.component';
import { TableCellComponent } from '../../../components/ui/table/table-cell.component';
import { TableHeaderComponent } from '../../../components/ui/table/table-header.component';
import { TableRowComponent } from '../../../components/ui/table/table-row.component';
import { TableComponent } from '../../../components/ui/table/table.component';

@Component({
	selector: 'app-category-list',
	templateUrl: './category-list.component.html',
	imports: [
		CommonModule,
		InputFieldComponent,
		ButtonComponent,
		TableComponent,
		TableHeaderComponent,
		TableBodyComponent,
		TableRowComponent,
		TableCellComponent,
		DropdownComponent,
		AlertComponent,
		DropdownItemComponent,
		RouterModule
	]
})
export class CategoryListComponent implements OnInit {
	categories: Category[] = [];
	totalCount = 0;
	pageNumber = 1;
	pageSize = 10;
	searchTerm = '';
	sortBy = 'name';
	sortDescending = false;
	parentCategoryId?: string;
	loading = false;

	constructor(private categoryService: CategoryService) {}

	ngOnInit(): void {
		this.loadCategories();
	}

	loadCategories(): void {
		this.loading = true;
		this.categoryService.getCategories({
			pageNumber: this.pageNumber,
			pageSize: this.pageSize,
			parentCategoryId: this.parentCategoryId,
			searchTerm: this.searchTerm,
			sortBy: this.sortBy,
			sortDescending: this.sortDescending
		}).subscribe({
			next: res => {
				this.categories = res.items;
				this.totalCount = res.totalCount;
				this.loading = false;
			},
			error: () => { this.loading = false; }
		});
	}

	deleteCategory(id: string): void {
		if (!confirm('Delete this category?')) return;
		this.categoryService.deleteCategory(id).subscribe({
			next: () => this.loadCategories()
		});
	}

	onSearch(term: string): void {
		this.searchTerm = term;
		this.pageNumber = 1;
		this.loadCategories();
	}

	onPageChange(page: number): void {
		this.pageNumber = page;
		this.loadCategories();
	}

	onSortChange(sortBy: string, sortDescending: boolean): void {
		this.sortBy = sortBy;
		this.sortDescending = sortDescending;
		this.loadCategories();
	}

	// Optionally, load hierarchy
	loadHierarchy(): void {
		this.categoryService.getCategoryHierarchy().subscribe();
	}
}
