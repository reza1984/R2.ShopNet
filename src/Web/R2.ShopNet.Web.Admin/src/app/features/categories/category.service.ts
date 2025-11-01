import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Category {
  id: string;
  name: string;
  description?: string;
  parentCategoryId?: string;
  // Add other fields as needed
}

export interface CategoryHierarchy {
  // Define hierarchy structure as needed
}

export interface CategoryListResponse {
  items: Category[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private baseUrl = '/api/Categories';

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

  createCategory(category: Partial<Category>): Observable<Category> {
    return this.http.post<Category>(`${this.baseUrl}`, category);
  }

  updateCategory(id: string, category: Partial<Category>): Observable<Category> {
    return this.http.put<Category>(`${this.baseUrl}/${id}`, { ...category, categoryId: id });
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
