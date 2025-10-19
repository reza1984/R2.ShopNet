export interface User {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  fullName: string;
  isActive: boolean;
  emailConfirmed: boolean;
  phoneNumber: string | null;
  lastLoginAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  roles: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UpdateUserRequest {
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
}
