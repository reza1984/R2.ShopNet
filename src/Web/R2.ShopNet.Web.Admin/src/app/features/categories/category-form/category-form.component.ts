
import { Component, Input, OnInit } from '@angular/core';
import { CategoryService } from '../category.service';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { ButtonComponent } from '../../../components/forms/button/button.component';
import { InputFieldComponent } from '../../../components/forms/input-field/input-field.component';
import { LabelComponent } from '../../../components/forms/label/label.component';

@Component({
	selector: 'app-category-form',
	templateUrl: './category-form.component.html',
	imports: [
		CommonModule,
		ReactiveFormsModule,
		InputFieldComponent,
		ButtonComponent,
        LabelComponent,
	]
})
export class CategoryFormComponent implements OnInit {
	@Input() categoryId?: string;
	form: FormGroup;
	loading = false;
	isEdit = false;

	constructor(
		private categoryService: CategoryService,
		private route: ActivatedRoute,
		public router: Router
	) {
		this.form = new FormGroup({
			name: new FormControl('', Validators.required),
			description: new FormControl(''),
			parentCategoryId: new FormControl('')
		});
	}

	ngOnInit(): void {
		if (this.categoryId) {
			this.isEdit = true;
			this.loadCategory(this.categoryId);
		} else {
			const id = this.route.snapshot.paramMap.get('id');
			if (id) {
				this.isEdit = true;
				this.loadCategory(id);
			}
		}
	}

	loadCategory(id: string): void {
		this.loading = true;
		this.categoryService.getCategoryById(id).subscribe({
			next: cat => {
				this.form.patchValue({
					name: cat.name,
					description: cat.description,
					parentCategoryId: cat.parentCategoryId
				});
				this.loading = false;
			},
			error: () => { this.loading = false; }
		});
	}

	saveCategory(): void {
		if (this.form.invalid) return;
		this.loading = true;
		const categoryData = this.form.value;
		if (this.isEdit && this.categoryId) {
			this.categoryService.updateCategory(this.categoryId, categoryData).subscribe({
				next: () => {
					this.loading = false;
					this.router.navigate(['/categories']);
				},
				error: () => { this.loading = false; }
			});
		} else {
			this.categoryService.createCategory(categoryData).subscribe({
				next: () => {
					this.loading = false;
					this.router.navigate(['/categories']);
				},
				error: () => { this.loading = false; }
			});
		}
	}

	onCancel(): void {
		this.router.navigate(['/categories']);
	}
}
