import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormField, FormRoot, form, required, submit } from '@angular/forms/signals';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [FormField, FormRoot, RouterLink],
  templateUrl: './forgot-password.component.html',
})
export class ForgotPasswordComponent {
  private readonly auth = inject(AuthService);

  protected readonly submitting = signal(false);
  protected readonly submitted = signal(false);

  protected readonly formModel = signal({ email: '' });

  protected readonly fields = form(this.formModel, (f) => {
    required(f.email, { message: 'Email is required.' });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.submitting.set(true);
      try {
        await this.auth.resetPassword(this.formModel().email);
        this.submitted.set(true);
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
