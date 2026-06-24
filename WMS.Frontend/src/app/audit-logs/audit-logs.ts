import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { normalizePagedResponse } from '../shared/pagination';
import { AuditLog, AuditLogsService } from './audit-logs.service';

@Component({
  selector: 'app-audit-logs',
  imports: [CommonModule, RouterLink],
  templateUrl: './audit-logs.html',
  styleUrl: './audit-logs.scss'
})
export class AuditLogs implements OnInit {
  private readonly auditLogsService = inject(AuditLogsService);

  protected logs: AuditLog[] = [];
  protected pageNumber = 1;
  protected pageSize = 10;
  protected totalPages = 0;
  protected totalCount = 0;
  protected isLoading = false;
  protected errorMessage = '';

  protected get hasPreviousPage(): boolean {
    return this.pageNumber > 1;
  }

  protected get hasNextPage(): boolean {
    return this.totalPages > 1 && this.pageNumber < this.totalPages;
  }

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.isLoading = true;
    this.auditLogsService.getAuditLogs(this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        const page = this.normalizePage(response);
        this.logs = page?.items ?? [];
        this.pageNumber = page?.pageNumber ?? this.pageNumber;
        this.pageSize = page?.pageSize ?? this.pageSize;
        this.totalPages = page?.totalPages ?? 0;
        this.totalCount = page?.totalCount ?? 0;
        this.errorMessage = '';
        this.isLoading = false;
      },
      error: (error) => {
        this.logs = [];
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to load audit logs.';
        this.isLoading = false;
      }
    });
  }

  changePage(delta: number): void {
    const nextPage = this.pageNumber + delta;
    if ((delta < 0 && !this.hasPreviousPage) || (delta > 0 && !this.hasNextPage) || nextPage < 1) {
      return;
    }

    this.pageNumber = nextPage;
    this.loadLogs();
  }

  protected formatDate(value: string): string {
    return new Date(value).toLocaleString();
  }

  private normalizePage(response: unknown): { items: AuditLog[]; totalCount: number; pageNumber: number; pageSize: number; totalPages: number } | null {
    return normalizePagedResponse<AuditLog>(response, this.pageNumber, this.pageSize);
  }
}
