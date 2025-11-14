import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ProductService, Product } from '../product.service';
import { CategoryService } from '../../categories/category.service';
import { ButtonComponent } from '../../../components/forms/button/button.component';
import { InputFieldComponent } from '../../../components/forms/input-field/input-field.component';
import { LabelComponent } from '../../../components/forms/label/label.component';
import { SelectComponent } from '../../../components/forms/select/select.component';
import { IconComponent } from '../../../components/icon/icon.component';
import { AlertComponent } from '../../../components/ui/alert/alert.component';
import { PageBreadcrumbComponent } from '../../../components/ui/page-breadcrumb/page-breadcrumb.component';
import { SlugHelper } from '../../../core/utils/slug.helper';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    ButtonComponent,
    InputFieldComponent,
    LabelComponent,
    SelectComponent,
    IconComponent,
    AlertComponent,
    PageBreadcrumbComponent
  ],
  templateUrl: './product-form.component.html'
})
export class ProductFormComponent implements OnInit {
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private route = inject(ActivatedRoute);
  public router = inject(Router);

  productId = signal<string | undefined>(undefined);
  loading = signal(false);
  isEdit = signal(false);
  errorMessage = signal('');
  autoGenerateSlug = signal(true);
  autoGenerateSku = signal(true);
  selectedImages = signal<File[]>([]);
  imagePreviewUrls = signal<string[]>([]);
  existingImages = signal<any[]>([]);

  categoriesApiUrl = `${environment.apiUrl}/api/catalog/Categories`;

  form: FormGroup = new FormGroup({
    name: new FormControl('', Validators.required),
    slug: new FormControl('', Validators.required),
    sku: new FormControl('', Validators.required),
    description: new FormControl(''),
    shortDescription: new FormControl(''),
    price: new FormControl(0, [Validators.required, Validators.min(0)]),
    currency: new FormControl('USD', Validators.required),
    discountPrice: new FormControl(null, [Validators.min(0)]),
    costPrice: new FormControl(null, [Validators.min(0)]),
    stockQuantity: new FormControl(0, [Validators.required, Validators.min(0)]),
    reorderLevel: new FormControl(10, [Validators.required, Validators.min(0)]),
    status: new FormControl('Draft', Validators.required),
    categoryId: new FormControl('', Validators.required),
    brand: new FormControl(''),
    weight: new FormControl(null, [Validators.min(0)]),
    dimensions: new FormControl(''),
    metaTitle: new FormControl(''),
    metaDescription: new FormControl(''),
    metaKeywords: new FormControl('')
  });

  productStatuses = [
    { value: 'Draft', label: 'Draft' },
    { value: 'Active', label: 'Active' },
    { value: 'Inactive', label: 'Inactive' },
    { value: 'OutOfStock', label: 'Out of Stock' },
    { value: 'Discontinued', label: 'Discontinued' }
  ];

  currencies = [
    { value: 'USD', label: 'USD ($)' },
    { value: 'EUR', label: 'EUR (€)' },
    { value: 'GBP', label: 'GBP (£)' },
    { value: 'JPY', label: 'JPY (¥)' }
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.productId.set(id);
      this.isEdit.set(true);
      this.loadProduct(id);
    }

    // Auto-generate slug from name
    this.form.get('name')?.valueChanges.subscribe(name => {
      if (this.autoGenerateSlug() && name) {
        this.form.get('slug')?.setValue(SlugHelper.generate(name), { emitEvent: false });
      }
      if (this.autoGenerateSku() && name) {
        const sku = this.generateSku(name);
        this.form.get('sku')?.setValue(sku, { emitEvent: false });
      }
    });

    // Disable auto-generation on manual edit
    // this.form.get('slug')?.valueChanges.subscribe(() => {
    //   if (document.activeElement === document.querySelector('input[formControlName="slug"]')) {
    //     this.autoGenerateSlug.set(false);
    //   }
    // });

    // this.form.get('sku')?.valueChanges.subscribe(() => {
    //   if (document.activeElement === document.querySelector('input[formControlName="sku"]')) {
    //     this.autoGenerateSku.set(false);
    //   }
    // }); 
  }

  generateSku(name: string): string {
    const prefix = name
      .split(' ')
      .slice(0, 3)
      .map(word => word.charAt(0).toUpperCase())
      .join('');
    const random = Math.floor(Math.random() * 10000).toString().padStart(4, '0');
    return `${prefix}-${random}`;
  }

  regenerateSlug(): void {
    const name = this.form.get('name')?.value;
    if (name) {
      this.form.get('slug')?.setValue(SlugHelper.generate(name));
      this.autoGenerateSlug.set(true);
    }
  }

  regenerateSku(): void {
    const name = this.form.get('name')?.value;
    if (name) {
      this.form.get('sku')?.setValue(this.generateSku(name));
      this.autoGenerateSku.set(true);
    }
  }

  loadProduct(id: string): void {
    this.loading.set(true);
    this.productService.getProductById(id).subscribe({
      next: (product) => {
        this.autoGenerateSlug.set(false);
        this.autoGenerateSku.set(false);
        this.productId.set(id);
        this.form.patchValue({
          name: product.name,
          slug: product.slug,
          sku: product.sku,
          description: product.description,
          shortDescription: product.shortDescription,
          price: product.price,
          currency: product.currency,
          discountPrice: product.discountPrice,
          stockQuantity: product.stockQuantity,
          reorderLevel: product.reorderLevel,
          status: product.status,
          categoryId: product.categoryId,
          brand: product.brand,
          weight: product.weight,
          dimensions: product.dimensions,
          metaTitle: product.metaTitle,
          metaDescription: product.metaDescription,
          metaKeywords: product.metaKeywords
        });

        if (product.images && product.images.length > 0) {
          this.existingImages.set(product.images);
        }

        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to load product');
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  saveProduct(): void {
    if (this.form.invalid) {
      Object.keys(this.form.controls).forEach(key => {
        this.form.get(key)?.markAsTouched();
      });
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    const productData = {
      ...this.form.value,
      price: parseFloat(this.form.value.price),
      discountPrice: this.form.value.discountPrice ? parseFloat(this.form.value.discountPrice) : null,
      costPrice: this.form.value.costPrice ? parseFloat(this.form.value.costPrice) : null,
      stockQuantity: parseInt(this.form.value.stockQuantity),
      reorderLevel: parseInt(this.form.value.reorderLevel),
      weight: this.form.value.weight ? parseFloat(this.form.value.weight) : null
    };

    const request = this.isEdit()
      ? this.productService.updateProduct(this.productId()!, productData)
      : this.productService.createProduct(productData);

    request.subscribe({
      next: (product) => {
        // Upload images if any
        if (this.selectedImages().length > 0) {
          this.uploadImages(product.id);
        } else {
          this.router.navigate(['/products']);
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to save product');
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  uploadImages(productId: string): void {
    const files = this.selectedImages();
    let uploadedCount = 0;

    files.forEach((file, index) => {
      this.productService.uploadProductImage(
        productId,
        file,
        file.name,
        index,
        index === 0
      ).subscribe({
        next: () => {
          uploadedCount++;
          if (uploadedCount === files.length) {
            this.loading.set(false);
            this.router.navigate(['/catalog/products']);
          }
        },
        error: (err) => {
          console.error('Failed to upload image:', err);
          uploadedCount++;
          if (uploadedCount === files.length) {
            this.loading.set(false);
            this.router.navigate(['/catalog/products']);
          }
        }
      });
    });
  }

  onFilesSelected(event: Event): void {
    const files = Array.from((event.target as HTMLInputElement).files || []);
    if (files.length === 0) return;

    const validFiles: File[] = [];
    const validPreviews: string[] = [];

    files.forEach(file => {
      // Validate type
      const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
      if (!allowedTypes.includes(file.type)) {
        this.errorMessage.set(`Invalid image type: ${file.name}`);
        return;
      }

      // Validate size (5MB max)
      if (file.size > 5 * 1024 * 1024) {
        this.errorMessage.set(`File too large: ${file.name} (max 5MB)`);
        return;
      }

      validFiles.push(file);

      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        validPreviews.push(e.target?.result as string);
        if (validPreviews.length === validFiles.length) {
          this.imagePreviewUrls.set([...this.imagePreviewUrls(), ...validPreviews]);
        }
      };
      reader.readAsDataURL(file);
    });

    this.selectedImages.set([...this.selectedImages(), ...validFiles]);
  }

  removeNewImage(index: number): void {
    const images = this.selectedImages();
    const previews = this.imagePreviewUrls();

    images.splice(index, 1);
    previews.splice(index, 1);

    this.selectedImages.set([...images]);
    this.imagePreviewUrls.set([...previews]);
  }

  removeExistingImage(imageId: string): void {
    if (!confirm('Are you sure you want to delete this image?')) {
      return;
    }

    this.productService.deleteProductImage(this.productId()!, imageId).subscribe({
      next: () => {
        const images = this.existingImages().filter(img => img.id !== imageId);
        this.existingImages.set(images);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to delete image');
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }
}
