import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormField, FormRoot, form, required, validate, submit } from '@angular/forms/signals';
import { AuthService } from '../../../../core/auth/auth.service';

type PageState = 'loading' | 'ready' | 'invalid-link' | 'success';

@Component({
  selector: 'app-update-password',
  standalone: true,
  imports: [FormField, FormRoot, RouterLink],
  templateUrl: './update-password.component.html',
})
export class UpdatePasswordComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly pageState = signal<PageState>('loading');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly formModel = signal({ password: '', confirmPassword: '' });

  protected readonly fields = form(this.formModel, (f) => {
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

  async ngOnInit(): Promise<void> {
    const code = this.route.snapshot.queryParamMap.get('code');
    if (!code) {
      this.pageState.set('invalid-link');
      return;
    }
    const { error } = await this.auth.exchangeCodeForSession(code);
    this.pageState.set(error ? 'invalid-link' : 'ready');
  }

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.submitting.set(true);
      this.error.set(null);
      try {
        const { error } = await this.auth.updatePassword(this.formModel().password);
        if (error) {
          this.error.set(error.message);
          return;
        }
        await this.auth.signOut();
        this.router.navigateByUrl('/auth/login');
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
