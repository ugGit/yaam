import { Component, inject, signal } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormField, FormRoot, form, required, submit } from '@angular/forms/signals';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormField, FormRoot, RouterLink],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly formModel = signal({ email: '', password: '' });

  protected readonly fields = form(this.formModel, (f) => {
    required(f.email, { message: 'Email is required.' });
    required(f.password, { message: 'Password is required.' });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.submitting.set(true);
      this.error.set(null);
      try {
        const model = this.formModel();
        const { error } = await this.auth.signIn(model.email, model.password);
        if (error) {
          this.error.set('Invalid email or password.');
          return;
        }
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/applications';
        this.router.navigateByUrl(returnUrl);
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
