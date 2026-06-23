import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';
import { ClientsService, Client, ClientRequest, PagedResult } from './clients.service';
import { AuthService } from '../auth/auth.service';

type ClientViewMode = 'list' | 'add' | 'detail' | 'edit';

@Component({
  selector: 'app-clients',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './clients.html',
  styleUrl: './clients.scss'
})
export class Clients implements OnInit {
  private readonly clientsService = inject(ClientsService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected mode: ClientViewMode = 'list';
  protected clients: Client[] = [];
  protected currentClient: Client | null = null;
  protected pageNumber = 1;
  protected pageSize = 100;
  protected totalPages = 0;
  protected totalCount = 0;
  protected isLoading = false;
  protected isSaving = false;
  protected message = '';
  protected errorMessage = '';
  protected search = '';
  protected statusFilter = 'all';
  protected readonly currentUser = this.authService.getCurrentUser();
  protected readonly canManage = this.isAdminRole(this.currentUser?.role);
  protected readonly navItems = [
    { label: 'Dashboard', route: '/dashboard' },
    { label: 'Employees', route: '/dashboard' },
    { label: 'Attendance', route: '/attendance' },
    { label: 'Leaves', route: '/leaves' },
    { label: 'Clients', route: '/clients' },
    { label: 'Projects', route: '/projects' },
    { label: 'Reports', route: '/reports' }
  ];

  protected readonly clientForm = this.formBuilder.group({
    clientName: ['', [Validators.required, Validators.maxLength(100)]],
    clientAddress: [''],
    clientPhoneNumber: ['', [Validators.maxLength(15), Validators.pattern(/^[0-9+\-\s()]*$/)]],
    clientLocation: ['', [Validators.maxLength(100)]],
    status: [true, [Validators.required]]
  });

  ngOnInit(): void {
    this.syncModeFromRoute();
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.syncModeFromRoute());
  }

  private syncModeFromRoute(): void {
    this.mode = this.resolveMode();
    this.message = '';
    this.errorMessage = '';
    this.currentClient = null;

    if (this.mode === 'list') {
      this.pageNumber = 1;
      this.loadClients();
    } else {
      const clientId = Number(this.route.snapshot.paramMap.get('id'));
      if (Number.isFinite(clientId) && clientId > 0) {
        this.loadClient(clientId);
      } else if (this.mode === 'add') {
        this.resetForm();
      }
    }
  }

  loadClients(): void {
    this.isLoading = true;
    const status = this.statusFilter === 'all' ? undefined : this.statusFilter === 'active';
    this.clientsService.getClients(this.search, status, this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        const page = this.normalizePage(response);
        this.clients = page?.items ?? [];
        this.totalCount = page?.totalCount ?? 0;
        this.totalPages = page?.totalPages ?? 0;
        this.pageNumber = page?.pageNumber ?? this.pageNumber;
        this.pageSize = page?.pageSize ?? this.pageSize;
        this.isLoading = false;
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = this.getApiError(error, 'Unable to load clients.');
      }
    });
  }

  searchClients(): void {
    this.pageNumber = 1;
    this.loadClients();
  }

  changePage(delta: number): void {
    const nextPage = this.pageNumber + delta;
    if (nextPage < 1 || (this.totalPages > 0 && nextPage > this.totalPages)) {
      return;
    }

    this.pageNumber = nextPage;
    this.loadClients();
  }

  saveClient(): void {
    if (!this.canManage) {
      this.errorMessage = 'Only admins can add or update clients.';
      return;
    }

    if (this.clientForm.invalid || this.isSaving) {
      this.clientForm.markAllAsTouched();
      return;
    }

    const payload = this.toRequest();
    const id = Number(this.route.snapshot.paramMap.get('id'));
    const request$ = this.mode === 'edit' && id > 0
      ? this.clientsService.updateClient(id, payload)
      : this.clientsService.createClient(payload);

    this.isSaving = true;
    this.message = '';
    this.errorMessage = '';
    request$.subscribe({
      next: (response) => {
        this.isSaving = false;
        this.message = response.message;
        this.errorMessage = '';
        if (this.mode === 'add') {
          void this.router.navigateByUrl('/clients');
        } else {
          this.loadClient(id);
        }
      },
      error: (error) => {
        this.isSaving = false;
        this.redirectIfUnauthorized(error);
        this.errorMessage = this.getApiError(error, 'Unable to save client.');
      }
    });
  }

  deactivate(client: Client): void {
    if (!this.canManage) {
      return;
    }

    this.clientsService.deactivateClient(client.clientId).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadClients();
      },
      error: (error) => {
        this.redirectIfUnauthorized(error);
        this.errorMessage = this.getApiError(error, 'Unable to deactivate client.');
      }
    });
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigateByUrl('/login');
  }

  private loadClient(clientId: number): void {
    this.clientsService.getClient(clientId).subscribe({
      next: (response) => {
        this.currentClient = response.data;
        if (this.mode === 'edit' && this.currentClient) {
          this.clientForm.patchValue({
            clientName: this.currentClient.clientName,
            clientAddress: this.currentClient.clientAddress ?? '',
            clientPhoneNumber: this.currentClient.clientPhoneNumber ?? '',
            clientLocation: this.currentClient.clientLocation ?? '',
            status: this.currentClient.status
          });
        }
      },
      error: (error) => {
        this.redirectIfUnauthorized(error);
        this.errorMessage = this.getApiError(error, 'Unable to load client.');
      }
    });
  }

  private resolveMode(): ClientViewMode {
    const path = this.route.snapshot.routeConfig?.path ?? 'clients';
    if (path.includes('add')) {
      return 'add';
    }
    if (path.includes('edit')) {
      return 'edit';
    }
    if (this.route.snapshot.paramMap.has('id')) {
      return 'detail';
    }
    return 'list';
  }

  private normalizePage(response: unknown): PagedResult<Client> | null {
    const source = response as { data?: unknown; Data?: unknown };
    const rawPage = this.toCamelCaseObject(source.data ?? source.Data) as Partial<PagedResult<Client>> | null;

    if (!rawPage) {
      return null;
    }

    return {
      items: rawPage.items ?? [],
      totalCount: Number(rawPage.totalCount ?? 0),
      pageNumber: Number(rawPage.pageNumber ?? this.pageNumber),
      pageSize: Number(rawPage.pageSize ?? this.pageSize),
      totalPages: Number(rawPage.totalPages ?? 0)
    };
  }

  private toCamelCaseObject(value: unknown): unknown {
    if (Array.isArray(value)) {
      return value.map((item) => this.toCamelCaseObject(item));
    }

    if (!value || typeof value !== 'object') {
      return value;
    }

    return Object.entries(value as Record<string, unknown>).reduce<Record<string, unknown>>((result, [key, item]) => {
      const normalizedKey = key.length ? `${key[0].toLowerCase()}${key.slice(1)}` : key;
      result[normalizedKey] = this.toCamelCaseObject(item);
      return result;
    }, {});
  }

  private resetForm(): void {
    this.clientForm.reset({
      clientName: '',
      clientAddress: '',
      clientPhoneNumber: '',
      clientLocation: '',
      status: true
    });
  }

  private toRequest(): ClientRequest {
    const value = this.clientForm.getRawValue();
    return {
      clientName: value.clientName?.trim() ?? '',
      clientAddress: value.clientAddress?.trim() || undefined,
      clientPhoneNumber: value.clientPhoneNumber?.trim() || undefined,
      clientLocation: value.clientLocation?.trim() || undefined,
      status: !!value.status
    };
  }

  private isAdminRole(role?: string): boolean {
    return role?.trim().toLowerCase() === 'admin';
  }

  private getApiError(error: unknown, fallback: string): string {
    const response = error as {
      status?: number;
      error?: { errors?: unknown; Errors?: unknown; message?: string; Message?: string; title?: string; Title?: string };
      message?: string;
    };

    if (response.status === 401) {
      return 'Your login session is missing or expired. Login again.';
    }

    if (response.status === 403) {
      return 'Only admins can perform this client action.';
    }

    const errors = response.error?.errors ?? response.error?.Errors;
    if (Array.isArray(errors) && errors.length > 0) {
      return String(errors[0]);
    }

    if (errors && typeof errors === 'object') {
      const first = Object.values(errors as Record<string, unknown>)[0];
      if (Array.isArray(first) && first.length > 0) {
        return String(first[0]);
      }
      if (first) {
        return String(first);
      }
    }

    return response.error?.message
      ?? response.error?.Message
      ?? response.error?.title
      ?? response.error?.Title
      ?? response.message
      ?? fallback;
  }

  private redirectIfUnauthorized(error: unknown): void {
    const response = error as { status?: number };
    if (response.status === 401) {
      this.authService.logout();
      void this.router.navigateByUrl('/login');
    }
  }
}
