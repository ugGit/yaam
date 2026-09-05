import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormField, FormRoot, form, required, validate, submit } from '@angular/forms/signals';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormField, FormRoot, RouterLink],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly registeredEmail = signal<string | null>(null);

  protected readonly formModel = signal({ email: '', password: '', confirmPassword: '' });

  protected readonly fields = form(this.formModel, (f) => {
    required(f.email, { message: 'Email is required.' });
    required(f.password, { message: 'Password is required.' });
    validate(f.password, (ctx) => {
      const value = ctx.value() as string;
      if (!value) return null;
      if (value.length < 12)
        return { kind: 'minLength', message: 'Password must be at least 12 characters.' };
      if (!/[0-9\W_]/.test(value))
        return {
          kind: 'complexity',
          message: 'Password must contain at least one number or special character.',
        };
      return null;
    });
    required(f.confirmPassword, { message: 'Please confirm your password.' });
    validate(f.confirmPassword, (ctx) => {
      const confirm = ctx.value() as string;
      const password = this.formModel().password;
      return confirm !== password ? { kind: 'mismatch', message: 'Passwords do not match.' } : null;
    });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.submitting.set(true);
      this.error.set(null);
      try {
        const model = this.formModel();
        const { data, error } = await this.auth.signUp(model.email, model.password);
        if (error) {
          this.error.set(error.message);
          return;
        }
        if (data.session) {
          this.router.navigateByUrl('/profile');
        } else {
          this.registeredEmail.set(model.email);
        }
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
