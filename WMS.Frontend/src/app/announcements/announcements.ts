import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { Announcement, AnnouncementRequest, AnnouncementsService, PagedResult } from './announcements.service';

type AnnouncementMode = 'list' | 'add' | 'edit' | 'detail';

@Component({
  selector: 'app-announcements',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './announcements.html',
  styleUrl: './announcements.scss'
})
export class Announcements implements OnInit {
  private readonly announcementsService = inject(AnnouncementsService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected mode: AnnouncementMode = 'list';
  protected announcements: Announcement[] = [];
  protected currentAnnouncement: Announcement | null = null;
  protected pageNumber = 1;
  protected pageSize = 100;
  protected totalPages = 0;
  protected totalCount = 0;
  protected isLoading = false;
  protected isSaving = false;
  protected message = '';
  protected errorMessage = '';
  protected readonly currentUser = this.authService.getCurrentUser();
  protected readonly canManage = this.currentUser?.role?.trim().toLowerCase() === 'admin';
  protected readonly targetRoles = ['', 'Admin', 'Manager', 'Employee'];

  protected readonly form = this.formBuilder.group({
    title: ['', [Validators.required, Validators.maxLength(150)]],
    message: ['', [Validators.required]],
    targetRole: [''],
    expiryDate: [''],
    isActive: [true, [Validators.required]]
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
    this.currentAnnouncement = null;

    if (this.mode === 'list') {
      this.pageNumber = 1;
      this.loadAnnouncements();
      return;
    }

    if (this.mode === 'add') {
      this.resetForm();
      return;
    }

    const id = Number(this.route.snapshot.paramMap.get('id'));
    if ((this.mode === 'edit' || this.mode === 'detail') && id > 0) {
      this.loadAnnouncement(id);
    }
  }

  loadAnnouncements(): void {
    this.isLoading = true;
    this.announcementsService.getAnnouncements(this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        const page = this.normalizePage(response);
        this.announcements = page?.items ?? [];
        this.pageNumber = page?.pageNumber ?? this.pageNumber;
        this.pageSize = page?.pageSize ?? this.pageSize;
        this.totalPages = page?.totalPages ?? 0;
        this.totalCount = page?.totalCount ?? 0;
        this.errorMessage = '';
        this.isLoading = false;
      },
      error: (error) => {
        this.redirectIfUnauthorized(error);
        this.announcements = [];
        this.errorMessage = this.getApiError(error, 'Unable to load announcements.');
        this.isLoading = false;
      }
    });
  }

  saveAnnouncement(): void {
    if (!this.canManage) {
      this.errorMessage = 'Only admins can create or update announcements.';
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage = 'Please enter a title and message before saving.';
      return;
    }

    if (this.isSaving) {
      return;
    }

    const id = Number(this.route.snapshot.paramMap.get('id'));
    const request$ = this.mode === 'edit' && id > 0
      ? this.announcementsService.updateAnnouncement(id, this.toRequest())
      : this.announcementsService.createAnnouncement(this.toRequest());

    this.isSaving = true;
    this.message = '';
    this.errorMessage = '';
    request$.subscribe({
      next: (response) => {
        this.isSaving = false;
        this.message = response.message;
        this.pageNumber = 1;
        this.resetForm();
        void this.router.navigateByUrl('/announcements').then((navigated) => {
          if (navigated) {
            this.mode = 'list';
            this.loadAnnouncements();
          }
        });
      },
      error: (error) => {
        this.redirectIfUnauthorized(error);
        this.isSaving = false;
        this.errorMessage = this.getApiError(error, 'Unable to save announcement.');
      }
    });
  }

  deactivate(announcement: Announcement): void {
    if (!this.canManage) {
      return;
    }

    this.announcementsService.deactivateAnnouncement(announcement.announcementId).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadAnnouncements();
      },
      error: (error) => {
        this.redirectIfUnauthorized(error);
        this.errorMessage = this.getApiError(error, 'Unable to deactivate announcement.');
      }
    });
  }

  changePage(delta: number): void {
    const nextPage = this.pageNumber + delta;
    if (nextPage < 1 || (this.totalPages > 0 && nextPage > this.totalPages)) {
      return;
    }

    this.pageNumber = nextPage;
    this.loadAnnouncements();
  }

  protected formatDate(value?: string): string {
    return value ? new Date(value).toLocaleDateString() : '--';
  }

  private loadAnnouncement(id: number): void {
    this.announcementsService.getAnnouncement(id).subscribe({
      next: (response) => {
        this.currentAnnouncement = response.data;
        if (this.mode === 'edit' && this.currentAnnouncement) {
          this.form.patchValue({
            title: this.currentAnnouncement.title,
            message: this.currentAnnouncement.message,
            targetRole: this.currentAnnouncement.targetRole ?? '',
            expiryDate: this.currentAnnouncement.expiryDate?.slice(0, 10) ?? '',
            isActive: this.currentAnnouncement.isActive
          });
        }
      },
      error: (error) => {
        this.redirectIfUnauthorized(error);
        this.errorMessage = this.getApiError(error, 'Unable to load announcement.');
      }
    });
  }

  private normalizePage(response: unknown): PagedResult<Announcement> | null {
    const source = response as { data?: unknown; Data?: unknown };
    const rawPage = this.toCamelCaseObject(source.data ?? source.Data) as Partial<PagedResult<Announcement>> | null;

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

  private resolveMode(): AnnouncementMode {
    const path = this.router.url.split('?')[0];
    if (path === '/announcements/add') {
      return 'add';
    }
    if (path.startsWith('/announcements/edit/')) {
      return 'edit';
    }
    if (/^\/announcements\/\d+$/.test(path)) {
      return 'detail';
    }
    return 'list';
  }

  private resetForm(): void {
    this.form.reset({
      title: '',
      message: '',
      targetRole: '',
      expiryDate: '',
      isActive: true
    });
  }

  private toRequest(): AnnouncementRequest {
    const value = this.form.getRawValue();
    return {
      title: value.title?.trim() ?? '',
      message: value.message?.trim() ?? '',
      targetRole: value.targetRole || undefined,
      expiryDate: value.expiryDate || undefined,
      isActive: !!value.isActive
    };
  }

  private redirectIfUnauthorized(error: unknown): void {
    const response = error as { status?: number };
    if (response.status === 401 || response.status === 403) {
      this.authService.logout();
      void this.router.navigateByUrl('/login');
    }
  }

  private getApiError(error: unknown, fallback: string): string {
    const response = error as {
      status?: number;
      error?: { errors?: unknown; message?: string; Message?: string; title?: string; Title?: string };
      message?: string;
    };

    if (response.status === 0) {
      return 'Cannot reach the backend API. Make sure the API is running on http://localhost:5000.';
    }

    if (response.status === 401 || response.status === 403) {
      return 'Your session is expired or unauthorized. Login again as Admin.';
    }

    const errors = response.error?.errors;
    if (Array.isArray(errors) && errors.length > 0) {
      return String(errors[0]);
    }

    return response.error?.message
      ?? response.error?.Message
      ?? response.error?.title
      ?? response.error?.Title
      ?? response.message
      ?? fallback;
  }
}
