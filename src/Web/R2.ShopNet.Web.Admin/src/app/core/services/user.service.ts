import { Injectable, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { User, PagedResult, UpdateUserRequest } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly apiUrl = `${environment.apiUrl}/api/identity/users`;
  
  // Signals for state management
  private usersSignal = signal<User[]>([]);
  private loadingSignal = signal<boolean>(false);
  private totalCountSignal = signal<number>(0);
  private currentPageSignal = signal<number>(1);
  private pageSizeSignal = signal<number>(20);
  
  // Public readonly signals
  readonly users = this.usersSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly totalCount = this.totalCountSignal.asReadonly();
  readonly currentPage = this.currentPageSignal.asReadonly();
  readonly pageSize = this.pageSizeSignal.asReadonly();
  
  // Computed signals
  readonly totalPages = computed(() => 
    Math.ceil(this.totalCountSignal() / this.pageSizeSignal())
  );
  readonly hasNextPage = computed(() => 
    this.currentPageSignal() < this.totalPages()
  );
  readonly hasPreviousPage = computed(() => 
    this.currentPageSignal() > 1
  );

  constructor(private http: HttpClient) {}

  getUsers(pageNumber: number = 1, pageSize: number = 20, searchTerm?: string, isActive?: boolean): Observable<PagedResult<User>> {
    this.loadingSignal.set(true);
    
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }
    
    if (isActive !== undefined) {
      params = params.set('isActive', isActive.toString());
    }

    return this.http.get<PagedResult<User>>(this.apiUrl, { params }).pipe(
      tap(result => {
        this.usersSignal.set(result.items);
        this.totalCountSignal.set(result.totalCount);
        this.currentPageSignal.set(result.pageNumber);
        this.pageSizeSignal.set(result.pageSize);
        this.loadingSignal.set(false);
      })
    );
  }

  getUserById(id: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/${id}`);
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, request);
  }

  deleteUser(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  activateUser(id: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/activate`, {});
  }

  deactivateUser(id: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/deactivate`, {});
  }

  // Helper methods to update page
  setPage(page: number): void {
    this.currentPageSignal.set(page);
  }

  nextPage(): void {
    if (this.hasNextPage()) {
      this.currentPageSignal.update(p => p + 1);
    }
  }

  previousPage(): void {
    if (this.hasPreviousPage()) {
      this.currentPageSignal.update(p => p - 1);
    }
  }
}
