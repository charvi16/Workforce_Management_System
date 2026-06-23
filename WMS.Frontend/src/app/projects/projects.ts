import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';
import { ClientsService, Client } from '../clients/clients.service';
import { EmployeeManagementService, Employee } from '../employees/employee-management.service';
import { AuthService } from '../auth/auth.service';
import {
  Project,
  ProjectAllocation,
  ProjectAllocationRequest,
  ProjectRequest,
  ProjectsService,
  PagedResult
} from './projects.service';

type ProjectViewMode = 'list' | 'add' | 'detail' | 'edit';

@Component({
  selector: 'app-projects',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './projects.html',
  styleUrl: './projects.scss'
})
export class Projects implements OnInit {
  private readonly projectsService = inject(ProjectsService);
  private readonly clientsService = inject(ClientsService);
  private readonly employeeService = inject(EmployeeManagementService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected mode: ProjectViewMode = 'list';
  protected projects: Project[] = [];
  protected currentProject: Project | null = null;
  protected allocations: ProjectAllocation[] = [];
  protected projectPageNumber = 1;
  protected projectPageSize = 10;
  protected projectTotalPages = 0;
  protected projectTotalCount = 0;
  protected allocationPageNumber = 1;
  protected allocationPageSize = 10;
  protected allocationTotalPages = 0;
  protected allocationTotalCount = 0;
  protected clients: Client[] = [];
  protected employees: Employee[] = [];
  protected isLoading = false;
  protected message = '';
  protected errorMessage = '';
  protected search = '';
  protected statusFilter = '';
  protected clientFilter = 0;
  protected readonly currentUser = this.authService.getCurrentUser();
  protected readonly canManage = this.isRole('Admin');
  protected readonly canAssign = this.isRole('Admin') || this.isRole('Manager');

  protected readonly statuses = ['Planned', 'Active', 'OnHold', 'Completed', 'Cancelled', 'Delayed'];

  protected readonly projectForm = this.formBuilder.group({
    projectName: ['', [Validators.required, Validators.maxLength(100)]],
    clientId: [null as number | null],
    startDate: [''],
    endDate: [''],
    status: ['Planned', [Validators.required]]
  });

  protected readonly allocationForm = this.formBuilder.group({
    empId: [0, [Validators.required, Validators.min(1)]],
    assignedOn: ['', [Validators.required]],
    roleInProject: [''],
    allocationPercentage: [null as number | null],
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
    this.currentProject = null;
    this.allocations = [];
    this.loadLookups();

    if (this.mode === 'list') {
      this.projectPageNumber = 1;
      this.loadProjects();
    } else {
      const projectId = Number(this.route.snapshot.paramMap.get('id'));
      if (Number.isFinite(projectId) && projectId > 0) {
        this.loadProject(projectId);
        this.loadAllocations(projectId);
      } else if (this.mode === 'add') {
        this.resetForms();
      }
    }
  }

  loadProjects(): void {
    this.isLoading = true;
    this.projectsService.getProjects(this.search, this.clientFilter || undefined, this.statusFilter, this.projectPageNumber, this.projectPageSize).subscribe({
      next: (response) => {
        const page = this.normalizeProjectPage(response);
        this.projects = page?.items ?? [];
        this.projectTotalCount = page?.totalCount ?? 0;
        this.projectTotalPages = page?.totalPages ?? 0;
        this.projectPageNumber = page?.pageNumber ?? this.projectPageNumber;
        this.projectPageSize = page?.pageSize ?? this.projectPageSize;
        this.isLoading = false;
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to load projects.';
        this.isLoading = false;
      }
    });
  }

  saveProject(): void {
    if (!this.canManage || this.projectForm.invalid) {
      this.projectForm.markAllAsTouched();
      return;
    }

    const payload = this.toProjectRequest();
    const id = Number(this.route.snapshot.paramMap.get('id'));
    const request$ = this.mode === 'edit' && id > 0
      ? this.projectsService.updateProject(id, payload)
      : this.projectsService.createProject(payload);

    request$.subscribe({
      next: (response) => {
        this.message = response.message;
        this.errorMessage = '';
        this.resetForms();
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to save project.';
      }
    });
  }

  cancelProject(projectId: number): void {
    if (!this.canManage) {
      return;
    }

    this.projectsService.cancelProject(projectId).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadProjects();
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to cancel project.';
      }
    });
  }

  updateStatus(projectId: number, status: string): void {
    this.projectsService.updateProjectStatus(projectId, status).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadProject(projectId);
        this.loadProjects();
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to update project status.';
      }
    });
  }

  assignEmployee(): void {
    if (!this.canAssign || this.allocationForm.invalid || !this.currentProject) {
      this.allocationForm.markAllAsTouched();
      return;
    }

    const value = this.allocationForm.getRawValue();
    const payload: ProjectAllocationRequest = {
      empId: Number(value.empId),
      projectId: this.currentProject.projectId,
      assignedOn: value.assignedOn ?? '',
      roleInProject: value.roleInProject?.trim() || undefined,
      allocationPercentage: value.allocationPercentage === null ? undefined : Number(value.allocationPercentage),
      status: !!value.status
    };

    this.projectsService.createAllocation(payload).subscribe({
      next: (response) => {
        this.message = response.message;
        this.errorMessage = '';
        this.allocationForm.patchValue({ roleInProject: '', allocationPercentage: null, status: true });
        this.loadAllocations(this.currentProject!.projectId);
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to assign employee.';
      }
    });
  }

  deactivateAllocation(allocationId: number): void {
    this.projectsService.deleteAllocation(allocationId).subscribe({
      next: (response) => {
        this.message = response.message;
        if (this.currentProject) {
          this.loadAllocations(this.currentProject.projectId);
        }
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to deactivate allocation.';
      }
    });
  }

  changeProjectPage(delta: number): void {
    const nextPage = this.projectPageNumber + delta;
    if (nextPage < 1 || (this.projectTotalPages > 0 && nextPage > this.projectTotalPages)) {
      return;
    }

    this.projectPageNumber = nextPage;
    this.loadProjects();
  }

  changeAllocationPage(delta: number): void {
    const nextPage = this.allocationPageNumber + delta;
    if (nextPage < 1 || (this.allocationTotalPages > 0 && nextPage > this.allocationTotalPages)) {
      return;
    }

    this.allocationPageNumber = nextPage;
    if (this.currentProject) {
      this.loadAllocations(this.currentProject.projectId);
    }
  }

  searchProjects(): void {
    this.projectPageNumber = 1;
    this.loadProjects();
  }

  private loadProject(projectId: number): void {
    this.projectsService.getProject(projectId).subscribe({
      next: (response) => {
        this.currentProject = response.data;
        if (this.mode === 'edit' && this.currentProject) {
          this.projectForm.patchValue({
            projectName: this.currentProject.projectName,
            clientId: this.currentProject.clientId ?? null,
            startDate: this.currentProject.startDate?.slice(0, 10) ?? '',
            endDate: this.currentProject.endDate?.slice(0, 10) ?? '',
            status: this.currentProject.status
          });
        }
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to load project.';
      }
    });
  }

  private loadAllocations(projectId: number): void {
    this.projectsService.getAllocationsByProject(projectId, this.allocationPageNumber, this.allocationPageSize).subscribe({
      next: (response) => {
        const page = this.normalizeAllocationPage(response);
        this.allocations = page?.items ?? [];
        this.allocationTotalCount = page?.totalCount ?? 0;
        this.allocationTotalPages = page?.totalPages ?? 0;
        this.allocationPageNumber = page?.pageNumber ?? this.allocationPageNumber;
        this.allocationPageSize = page?.pageSize ?? this.allocationPageSize;
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to load project allocations.';
      }
    });
  }

  private loadLookups(): void {
    this.clientsService.getClients('', true, 1, 100).subscribe({
      next: (response) => this.clients = response.data?.items ?? [],
      error: () => this.clients = []
    });

    this.employeeService.getEmployees('', '', '', '', 1, 100).subscribe({
      next: (response) => this.employees = response.data?.items ?? [],
      error: () => this.employees = []
    });
  }

  private resolveMode(): ProjectViewMode {
    const path = this.route.snapshot.routeConfig?.path ?? 'projects';
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

  private normalizeProjectPage(response: unknown): PagedResult<Project> | null {
    return this.normalizePage<Project>(response, this.projectPageNumber, this.projectPageSize);
  }

  private normalizeAllocationPage(response: unknown): PagedResult<ProjectAllocation> | null {
    return this.normalizePage<ProjectAllocation>(response, this.allocationPageNumber, this.allocationPageSize);
  }

  private normalizePage<T>(response: unknown, fallbackPageNumber: number, fallbackPageSize: number): PagedResult<T> | null {
    const source = response as { data?: unknown; Data?: unknown };
    const rawPage = this.toCamelCaseObject(source.data ?? source.Data) as Partial<PagedResult<T>> | null;

    if (!rawPage) {
      return null;
    }

    return {
      items: rawPage.items ?? [],
      totalCount: Number(rawPage.totalCount ?? 0),
      pageNumber: Number(rawPage.pageNumber ?? fallbackPageNumber),
      pageSize: Number(rawPage.pageSize ?? fallbackPageSize),
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

  private isRole(role: string): boolean {
    return this.currentUser?.role?.trim().toLowerCase() === role.toLowerCase();
  }

  private resetForms(): void {
    this.projectForm.reset({
      projectName: '',
      clientId: null,
      startDate: '',
      endDate: '',
      status: 'Planned'
    });
    this.allocationForm.reset({
      empId: 0,
      assignedOn: '',
      roleInProject: '',
      allocationPercentage: null,
      status: true
    });
  }

  private toProjectRequest(): ProjectRequest {
    const value = this.projectForm.getRawValue();
    return {
      projectName: value.projectName ?? '',
      clientId: value.clientId ?? null,
      startDate: value.startDate || null,
      endDate: value.endDate || null,
      status: value.status ?? 'Planned'
    };
  }
}
