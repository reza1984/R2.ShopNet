import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Product {
  id: string;
  name: string;
  slug: string;
  sku: string;
  description?: string;
  shortDescription?: string;
  price: number;
  currency: string;
  discountPrice?: number;
  discountPercentage?: number;
  stockQuantity: number;
  reorderLevel: number;
  status: string;
  categoryId: string;
  categoryName?: string;
  brand?: string;
  weight?: number;
  dimensions?: string;
  images?: ProductImage[];
  variants?: ProductVariant[];
  metaTitle?: string;
  metaDescription?: string;
  metaKeywords?: string;
  viewCount: number;
  averageRating: number;
  reviewCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface ProductImage {
  id: string;
  url: string;
  fileName: string;
  contentType: string;
  sizeInBytes: number;
  altText?: string;
  displayOrder: number;
  isPrimary: boolean;
}

export interface ProductVariant {
  id: string;
  name: string;
  sku: string;
  price?: number;
  currency?: string;
  stockQuantity: number;
  weight?: number;
  attributes: Record<string, string>;
  imageUrl?: string;
  isActive: boolean;
}

export interface ProductListResponse {
  items: Product[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CreateProductRequest {
  name: string;
  slug: string;
  sku: string;
  description?: string;
  shortDescription?: string;
  price: number;
  currency: string;
  discountPrice?: number;
  costPrice?: number;
  stockQuantity: number;
  reorderLevel: number;
  status: string;
  categoryId: string;
  brand?: string;
  weight?: number;
  dimensions?: string;
  metaTitle?: string;
  metaDescription?: string;
  metaKeywords?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;
  private baseUrl = `${this.apiUrl}/api/catalog/Products`;

  getProducts(params: {
    pageNumber: number;
    pageSize: number;
    categoryId?: string;
    searchTerm?: string;
    status?: string;
    sortBy?: string;
    sortDescending?: boolean;
  }): Observable<ProductListResponse> {
    let httpParams = new HttpParams()
      .set('pageNumber', params.pageNumber.toString())
      .set('pageSize', params.pageSize.toString())
      .set('sortDescending', params.sortDescending?.toString() || 'false');

    if (params.categoryId) {
      httpParams = httpParams.set('categoryId', params.categoryId);
    }
    if (params.searchTerm) {
      httpParams = httpParams.set('searchTerm', params.searchTerm);
    }
    if (params.status) {
      httpParams = httpParams.set('status', params.status);
    }
    if (params.sortBy) {
      httpParams = httpParams.set('sortBy', params.sortBy);
    }

    return this.http.get<ProductListResponse>(this.baseUrl, { params: httpParams });
  }

  getProductById(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  createProduct(productData: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, productData);
  }

  updateProduct(id: string, productData: Partial<CreateProductRequest>): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, {...productData, ProductId: id});
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  uploadProductImage(
    productId: string,
    file: File,
    altText?: string,
    displayOrder: number = 0,
    isPrimary: boolean = false
  ): Observable<ProductImage> {
    const formData = new FormData();
    formData.append('file', file);
    if (altText) {
      formData.append('altText', altText);
    }
    formData.append('displayOrder', displayOrder.toString());
    formData.append('isPrimary', isPrimary.toString());

    return this.http.post<ProductImage>(
      `${this.baseUrl}/${productId}/images`,
      formData
    );
  }

  getProductImages(productId: string, expiryMinutes: number = 10080): Observable<ProductImage[]> {
    const params = new HttpParams().set('expiryMinutes', expiryMinutes.toString());
    return this.http.get<ProductImage[]>(`${this.baseUrl}/${productId}/images`, { params });
  }

  deleteProductImage(productId: string, imageId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${productId}/images/${imageId}`);
  }

  setPrimaryImage(productId: string, imageId: string): Observable<boolean> {
    return this.http.patch<boolean>(
      `${this.baseUrl}/${productId}/images/${imageId}/set-primary`,
      {}
    );
  }
}
