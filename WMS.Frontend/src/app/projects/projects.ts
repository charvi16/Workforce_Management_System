import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, firstValueFrom } from 'rxjs';
import { ClientsService, Client } from '../clients/clients.service';
import { EmployeeManagementService, Employee } from '../employees/employee-management.service';
import { AuthService } from '../auth/auth.service';
import { normalizePagedResponse } from '../shared/pagination';
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
  protected isSaving = false;
  protected isAssigning = false;
  protected message = '';
  protected errorMessage = '';
  protected search = '';
  protected statusFilter = '';
  protected clientFilter = 0;
  protected selectedProjectMemberIds = new Set<number>();
  protected readonly currentUser = this.authService.getCurrentUser();
  protected readonly canManage = this.isRole('Admin');
  protected readonly canAssign = this.isRole('Admin') || this.isRole('Manager');

  protected readonly statuses = ['Planned', 'Active', 'OnHold', 'Completed', 'Cancelled', 'Delayed'];

  protected get hasPreviousProjectPage(): boolean {
    return this.projectPageNumber > 1;
  }

  protected get hasNextProjectPage(): boolean {
    return this.projectTotalPages > 1 && this.projectPageNumber < this.projectTotalPages;
  }

  protected get hasPreviousAllocationPage(): boolean {
    return this.allocationPageNumber > 1;
  }

  protected get hasNextAllocationPage(): boolean {
    return this.allocationTotalPages > 1 && this.allocationPageNumber < this.allocationTotalPages;
  }

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

  protected get selectedProjectMemberCount(): number {
    return this.selectedProjectMemberIds.size;
  }

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
    this.selectedProjectMemberIds.clear();
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

  async saveProject(): Promise<void> {
    if (!this.canManage || this.projectForm.invalid) {
      this.projectForm.markAllAsTouched();
      return;
    }

    if (this.isSaving) {
      return;
    }

    const payload = this.toProjectRequest();
    const id = Number(this.route.snapshot.paramMap.get('id'));
    const request$ = this.mode === 'edit' && id > 0
      ? this.projectsService.updateProject(id, payload)
      : this.projectsService.createProject(payload);

    this.isSaving = true;
    this.message = '';
    this.errorMessage = '';

    try {
      const response = await firstValueFrom(request$);
      const project = response.data;
      await this.createSelectedMemberAllocations(project.projectId);
      this.message = this.selectedProjectMemberCount > 0
        ? `${response.message} ${this.selectedProjectMemberCount} member(s) assigned.`
        : response.message;
      this.errorMessage = '';
      this.selectedProjectMemberIds.clear();

      if (this.mode === 'add') {
        void this.router.navigateByUrl(`/projects/${project.projectId}`);
      } else {
        this.currentProject = project;
        this.loadProject(project.projectId);
        this.loadAllocations(project.projectId);
      }
    } catch (error) {
      this.errorMessage = this.getApiError(error, 'Unable to save project.');
    } finally {
      this.isSaving = false;
    }
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
      this.errorMessage = !this.currentProject ? 'Open a project before assigning employees.' : 'Select an employee and assigned date.';
      return;
    }

    this.isAssigning = true;
    this.message = '';
    this.errorMessage = '';
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
        this.allocationForm.patchValue({
          empId: 0,
          assignedOn: this.defaultAssignedOnDate(this.currentProject),
          roleInProject: '',
          allocationPercentage: null,
          status: true
        });
        this.loadAllocations(this.currentProject!.projectId);
        this.isAssigning = false;
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to assign employee.';
        this.isAssigning = false;
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
    if ((delta < 0 && !this.hasPreviousProjectPage) || (delta > 0 && !this.hasNextProjectPage) || nextPage < 1) {
      return;
    }

    this.projectPageNumber = nextPage;
    this.loadProjects();
  }

  changeAllocationPage(delta: number): void {
    const nextPage = this.allocationPageNumber + delta;
    if ((delta < 0 && !this.hasPreviousAllocationPage) || (delta > 0 && !this.hasNextAllocationPage) || nextPage < 1) {
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

  protected isProjectMemberSelected(employeeId: number): boolean {
    return this.selectedProjectMemberIds.has(employeeId);
  }

  protected toggleProjectMember(employeeId: number, isSelected: boolean): void {
    if (isSelected) {
      this.selectedProjectMemberIds.add(employeeId);
      return;
    }

    this.selectedProjectMemberIds.delete(employeeId);
  }

  private loadProject(projectId: number): void {
    this.projectsService.getProject(projectId).subscribe({
      next: (response) => {
        this.currentProject = response.data;
        this.resetAllocationFormForProject(this.currentProject);
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
        if (this.mode === 'edit') {
          this.selectedProjectMemberIds = new Set(this.allocations.filter((allocation) => allocation.status).map((allocation) => allocation.empId));
        }
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
      next: (response) => this.clients = this.normalizePage<Client>(response, 1, 100)?.items ?? [],
      error: () => this.clients = []
    });

    this.employeeService.getEmployees('', '', '', '', 1, 100).subscribe({
      next: (response) => this.employees = this.normalizePage<Employee>(response, 1, 100)?.items ?? [],
      error: () => this.employees = []
    });
  }

  protected employeeDisplayName(employee: Employee): string {
    const fullName = employee.fullName?.trim();
    if (fullName) {
      return fullName;
    }

    const firstLast = `${employee.firstName ?? ''} ${employee.lastName ?? ''}`.trim();
    return firstLast || employee.username || `Employee ${employee.employeeId}`;
  }

  protected projectStartDate(): string | null {
    return this.currentProject?.startDate?.slice(0, 10) ?? null;
  }

  protected projectEndDate(): string | null {
    return this.currentProject?.endDate?.slice(0, 10) ?? null;
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
    return normalizePagedResponse<T>(response, fallbackPageNumber, fallbackPageSize);
  }

  private isRole(role: string): boolean {
    return this.currentUser?.role?.trim().toLowerCase() === role.toLowerCase();
  }

  private async createSelectedMemberAllocations(projectId: number): Promise<void> {
    if (this.selectedProjectMemberIds.size === 0) {
      return;
    }

    const existingActiveMemberIds = new Set(this.allocations.filter((allocation) => allocation.status).map((allocation) => allocation.empId));
    const memberIds = [...this.selectedProjectMemberIds].filter((employeeId) => !existingActiveMemberIds.has(employeeId));
    if (memberIds.length === 0) {
      return;
    }

    const assignedOn = this.defaultAssignedOnDateForForm();
    await Promise.all(memberIds.map((employeeId) => firstValueFrom(this.projectsService.createAllocation({
      empId: employeeId,
      projectId,
      assignedOn,
      status: true
    }))));
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
      assignedOn: this.defaultAssignedOnDate(this.currentProject),
      roleInProject: '',
      allocationPercentage: null,
      status: true
    });
    this.selectedProjectMemberIds.clear();
  }

  private resetAllocationFormForProject(project: Project | null): void {
    if (!project || !this.canAssign) {
      return;
    }

    this.allocationForm.reset({
      empId: 0,
      assignedOn: this.defaultAssignedOnDate(project),
      roleInProject: '',
      allocationPercentage: null,
      status: true
    });
  }

  private defaultAssignedOnDate(project: Project | null): string {
    const today = new Date().toISOString().slice(0, 10);
    const startDate = project?.startDate?.slice(0, 10);
    const endDate = project?.endDate?.slice(0, 10);

    if (startDate && today < startDate) {
      return startDate;
    }

    if (endDate && today > endDate) {
      return endDate;
    }

    return today;
  }

  private defaultAssignedOnDateForForm(): string {
    const today = new Date().toISOString().slice(0, 10);
    const startDate = this.projectForm.controls.startDate.value || undefined;
    const endDate = this.projectForm.controls.endDate.value || undefined;

    if (startDate && today < startDate) {
      return startDate;
    }

    if (endDate && today > endDate) {
      return endDate;
    }

    return today;
  }

  private getApiError(error: unknown, fallback: string): string {
    const response = error as {
      error?: { errors?: unknown; message?: string; Message?: string; title?: string; Title?: string };
      message?: string;
    };
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
