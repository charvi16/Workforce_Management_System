import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class Register {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);

  protected readonly roles = [
    { id: 1, name: 'Admin' },
    { id: 2, name: 'Manager' },
    { id: 3, name: 'Employee' }
  ];

  protected readonly form = this.formBuilder.group({
    username: ['', [Validators.required, Validators.maxLength(50)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    roleId: [3, [Validators.required]]
  });

  protected isSubmitting = false;
  protected errorMessage = '';
  protected successMessage = '';

  submit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    const { username, password, roleId } = this.form.getRawValue();
    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const newUsername = username ?? '';
    const newPassword = password ?? '';

    this.authService.register(newUsername, newPassword, Number(roleId)).pipe(
      switchMap(() => this.authService.login(newUsername, newPassword))
    ).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        this.successMessage = `${response.data.username} registered as ${response.data.role}.`;
        void this.router.navigateByUrl('/dashboard');
      },
      error: (error) => {
        this.isSubmitting = false;
        this.errorMessage = error.error?.errors?.[0] ?? error.error?.message ?? 'Unable to create this account.';
      }
    });
  }
}
