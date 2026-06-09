import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);

  protected readonly form = this.formBuilder.group({
    username: ['', [Validators.required, Validators.maxLength(50)]],
    password: ['', [Validators.required]]
  });

  protected isSubmitting = false;
  protected errorMessage = '';

  submit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    const { username, password } = this.form.getRawValue();
    this.isSubmitting = true;
    this.errorMessage = '';

    this.authService.login(username ?? '', password ?? '').subscribe({
      next: () => {
        this.isSubmitting = false;
        void this.router.navigateByUrl('/');
      },
      error: (error) => {
        this.isSubmitting = false;
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to login with the provided credentials.';
      }
    });
  }
}
