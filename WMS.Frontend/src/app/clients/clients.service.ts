import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/auth.service';

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface Client {
  clientId: number;
  clientName: string;
  clientAddress?: string;
  clientPhoneNumber?: string;
  clientLocation?: string;
  status: boolean;
  projectCount: number;
}

export interface ClientRequest {
  clientName: string;
  clientAddress?: string;
  clientPhoneNumber?: string;
  clientLocation?: string;
  status: boolean;
}

@Injectable({ providedIn: 'root' })
export class ClientsService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  getClients(search = '', status?: boolean, pageNumber = 1, pageSize = 10): Observable<ApiResponse<PagedResult<Client>>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    if (status !== undefined && status !== null) {
      params = params.set('status', status);
    }

    return this.http.get<ApiResponse<PagedResult<Client>>>(`${environment.apiBaseUrl}/Clients`, {
      params,
      headers: this.getAuthHeaders()
    });
  }

  getClient(clientId: number): Observable<ApiResponse<Client>> {
    return this.http.get<ApiResponse<Client>>(`${environment.apiBaseUrl}/Clients/${clientId}`, {
      headers: this.getAuthHeaders()
    });
  }

  createClient(request: ClientRequest): Observable<ApiResponse<Client>> {
    return this.http.post<ApiResponse<Client>>(`${environment.apiBaseUrl}/Clients`, request, {
      headers: this.getAuthHeaders()
    });
  }

  updateClient(clientId: number, request: ClientRequest): Observable<ApiResponse<Client>> {
    return this.http.put<ApiResponse<Client>>(`${environment.apiBaseUrl}/Clients/${clientId}`, request, {
      headers: this.getAuthHeaders()
    });
  }

  deactivateClient(clientId: number): Observable<ApiResponse<Client>> {
    return this.http.delete<ApiResponse<Client>>(`${environment.apiBaseUrl}/Clients/${clientId}`, {
      headers: this.getAuthHeaders()
    });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
