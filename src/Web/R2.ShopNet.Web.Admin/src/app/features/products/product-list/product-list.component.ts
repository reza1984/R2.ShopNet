import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProductService, Product } from '../product.service';
import { ButtonComponent } from '../../../components/forms/button/button.component';
import { IconComponent } from '../../../components/icon/icon.component';
import { AlertComponent } from '../../../components/ui/alert/alert.component';
import { SelectComponent, Option } from '../../../components/forms/select/select.component';
import { ConfirmationModalComponent } from '../../../components/ui/confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonComponent,
    IconComponent,
    AlertComponent,
    SelectComponent,
    ConfirmationModalComponent
  ],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {
  private productService = inject(ProductService);

  products = signal<Product[]>([]);
  totalCount = signal(0);
  pageNumber = signal(1);
  pageSize = signal(10);
  searchTerm = signal('');
  sortBy = signal('name');
  sortDescending = signal(false);
  categoryId = signal<string | undefined>(undefined);
  status = signal<string | undefined>(undefined);
  loading = signal(false);
  errorMessage = signal<string>('');

  // Confirmation modal
  showDeleteConfirmation = signal(false);
  productToDelete = signal<{ id: string; name: string } | null>(null);

  Math = Math;

  statusOptions: Option[] = [
    { value: '', label: 'All Status' },
    { value: 'Active', label: 'Active' },
    { value: 'Draft', label: 'Draft' },
    { value: 'Inactive', label: 'Inactive' },
    { value: 'OutOfStock', label: 'Out of Stock' },
    { value: 'Discontinued', label: 'Discontinued' }
  ];

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.productService.getProducts({
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      categoryId: this.categoryId(),
      searchTerm: this.searchTerm(),
      status: this.status(),
      sortBy: this.sortBy(),
      sortDescending: this.sortDescending()
    }).subscribe({
      next: (response) => {
        this.products.set(response.items);
        this.totalCount.set(response.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to load products');
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  confirmDeleteProduct(id: string, name: string): void {
    this.productToDelete.set({ id, name });
    this.showDeleteConfirmation.set(true);
  }

  deleteProduct(): void {
    const product = this.productToDelete();
    if (!product) return;

    this.errorMessage.set('');
    this.showDeleteConfirmation.set(false);
    this.productService.deleteProduct(product.id).subscribe({
      next: () => {
        this.loadProducts();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to delete product');
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  cancelDelete(): void {
    this.showDeleteConfirmation.set(false);
    this.productToDelete.set(null);
  }

  onSearch(term: string): void {
    this.searchTerm.set(term);
    this.pageNumber.set(1);
    this.loadProducts();
  }

  onPageChange(page: number): void {
    this.pageNumber.set(page);
    this.loadProducts();
  }

  onSortChange(sortBy: string, sortDescending: boolean): void {
    this.sortBy.set(sortBy);
    this.sortDescending.set(sortDescending);
    this.loadProducts();
  }

  onStatusFilter(status: string): void {
    this.status.set(status || undefined);
    this.pageNumber.set(1);
    this.loadProducts();
  }

  getPrimaryImage(product: Product): string | undefined {
    return product.images?.find(img => img.isPrimary)?.url || product.images?.[0]?.url;
  }

  getStatusBadgeClass(status: string): string {
    const statusMap: Record<string, string> = {
      'Active': 'bg-success-100 text-success-700',
      'Draft': 'bg-gray-100 text-gray-700',
      'Inactive': 'bg-warning-100 text-warning-700',
      'OutOfStock': 'bg-error-100 text-error-700',
      'Discontinued': 'bg-error-100 text-error-700'
    };
    return statusMap[status] || 'bg-gray-100 text-gray-700';
  }

  formatPrice(price: number, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency || 'USD'
    }).format(price);
  }

  getStockStatusClass(quantity: number, reorderLevel: number): string {
    if (quantity === 0) return 'text-error-600';
    if (quantity <= reorderLevel) return 'text-warning-600';
    return 'text-success-600';
  }
}
