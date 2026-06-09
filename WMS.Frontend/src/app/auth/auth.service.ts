import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

interface AuthResponse {
  userId: number;
  username: string;
  role: string;
  token: string;
  expiresAtUtc: string;
}

export interface CurrentUser {
  userId: number;
  username: string;
  role: string;
  expiresAtUtc: string;
}

interface RegisterResponse {
  userId: number;
  username: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenKey = 'wms_token';
  private readonly userKey = 'wms_user';

  login(username: string, password: string): Observable<ApiResponse<AuthResponse>> {
    return this.http
      .post<ApiResponse<AuthResponse>>(`${environment.apiBaseUrl}/Auth/login`, { username, password })
      .pipe(
        tap((response) => {
          if (response.success && response.data?.token) {
            localStorage.setItem(this.tokenKey, response.data.token);
            localStorage.setItem(this.userKey, JSON.stringify({
              userId: response.data.userId,
              username: response.data.username,
              role: response.data.role,
              expiresAtUtc: response.data.expiresAtUtc
            }));
          }
        })
      );
  }

  register(username: string, password: string, roleId: number): Observable<ApiResponse<RegisterResponse>> {
    return this.http.post<ApiResponse<RegisterResponse>>(`${environment.apiBaseUrl}/Auth/register`, {
      username,
      password,
      roleId
    });
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getCurrentUser(): CurrentUser | null {
    const userJson = localStorage.getItem(this.userKey);
    if (!userJson) {
      return null;
    }

    try {
      return JSON.parse(userJson) as CurrentUser;
    } catch {
      this.logout();
      return null;
    }
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
  }
}
