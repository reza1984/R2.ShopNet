import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  parentCategoryId?: string;
  parentCategoryName?: string;
  displayOrder: number;
  imageUrl?: string;
}

export interface CategoryHierarchy {
  // Define hierarchy structure as needed
}

export interface CategoryListResponse {
  items: Category[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly apiUrl = environment.apiUrl;
  private baseUrl = `${this.apiUrl}/api/catalog/Categories`;

  constructor(private http: HttpClient) {}

  getCategories(params: {
    pageNumber: number;
    pageSize: number;
    parentCategoryId?: string;
    searchTerm?: string;
    sortBy?: string;
    sortDescending?: boolean;
  }): Observable<CategoryListResponse> {
    let httpParams = new HttpParams()
      .set('pageNumber', params.pageNumber)
      .set('pageSize', params.pageSize);
    if (params.parentCategoryId) httpParams = httpParams.set('parentCategoryId', params.parentCategoryId);
    if (params.searchTerm) httpParams = httpParams.set('searchTerm', params.searchTerm);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDescending !== undefined) httpParams = httpParams.set('sortDescending', params.sortDescending);
    return this.http.get<CategoryListResponse>(`${this.baseUrl}`, { params: httpParams });
  }

  getCategoryById(id: string): Observable<Category> {
    return this.http.get<Category>(`${this.baseUrl}/${id}`);
  }

  getCategoryHierarchy(): Observable<CategoryHierarchy> {
    return this.http.get<CategoryHierarchy>(`${this.baseUrl}/hierarchy`);
  }

  createCategory(categoryData: FormData | Partial<Category>): Observable<Category> {
    return this.http.post<Category>(`${this.baseUrl}`, categoryData);
  }

  updateCategory(id: string, categoryData: FormData | Partial<Category>): Observable<Category> {
    if (categoryData instanceof FormData) {
      return this.http.put<Category>(`${this.baseUrl}/${id}`, categoryData);
    }
    return this.http.put<Category>(`${this.baseUrl}/${id}`, { ...categoryData, categoryId: id });
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  uploadCategoryImage(categoryId: string, file: File, altText?: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    if (altText) {
      formData.append('altText', altText);
    }
    return this.http.post(`${this.baseUrl}/${categoryId}/images`, formData);
  }

  deleteCategoryImage(categoryId: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${categoryId}/images`);
  }
}
