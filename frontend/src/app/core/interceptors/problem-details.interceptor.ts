import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface ProblemDetails {
  title: string;
  status: number;
  errors?: Record<string, string[]>;
}

export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const problem: ProblemDetails = error.error ?? {
        title: 'An unexpected error occurred.',
        status: error.status,
      };
      return throwError(() => problem);
    })
  );
